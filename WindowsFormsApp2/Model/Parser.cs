using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2.Model
{
    public class Parser
    {
        private enum ParserState
        {
            Start, IdRem, AfterEqual, OpenQuote, InFormatSpecifier,
            CloseQuote, AfterDot, Format, OpenArg, InNumber,
            AfterNumber, CloseArg, End, Error
        }

        public class SyntaxError
        {
            public string Fragment { get; set; }
            public int Line { get; set; }
            public int Position { get; set; }
            public string Description { get; set; }
        }

        private ParserState currentState;
        private List<Lexem> lexems;
        private int position;
        private List<string> stateLog;
        private List<SyntaxError> errors;
        private bool hasNumberInCurrentState;
        private int currentLine;
        private int lastErrorPositionInLine;
        private bool errorFoundInCurrentStatement;

        private int formatSpecifierStep;
        private int formatSpecifierStartPosition;
        private bool formatSpecifierErrorReported;

        private bool skipToNextStatement;
        public Parser(List<Lexem> lexems)
        {
            this.lexems = lexems ?? new List<Lexem>();
            this.position = 0;
            this.currentState = ParserState.Start;
            this.stateLog = new List<string>();
            this.errors = new List<SyntaxError>();
            this.hasNumberInCurrentState = false;
            this.currentLine = 1;
            this.lastErrorPositionInLine = -1;
            this.errorFoundInCurrentStatement = false;
            this.formatSpecifierStep = 0;
            this.formatSpecifierStartPosition = -1;
            this.formatSpecifierErrorReported = false;
        }

        private void ResetFormatSpecifier()
        {
            formatSpecifierStep = 0;
            formatSpecifierStartPosition = -1;
            formatSpecifierErrorReported = false;
        }

        private void ResetToStart()
        {
            currentState = ParserState.Start;
            ResetFormatSpecifier();
            hasNumberInCurrentState = false;
        }

        public bool Parse()
        {
            errors.Clear();
            currentState = ParserState.Start;
            position = 0;
            currentLine = 1;
            lastErrorPositionInLine = -1;
            errorFoundInCurrentStatement = false;
            ResetFormatSpecifier();

            while (position < lexems.Count)
            {
                Lexem currentLexem = lexems[position];

                if (currentLexem.lexemLine > currentLine)
                {
                    currentLine = currentLexem.lexemLine;
                    errorFoundInCurrentStatement = false;
                    lastErrorPositionInLine = -1;
                    ResetToStart();
                    stateLog.Add($"--- Новая строка {currentLine} ---");
                }

                // Проверяем ожидаемую лексему
                if (IsExpectedLexem(currentState, currentLexem))
                {
                    ProcessLexem(currentLexem);
                    position++;
                }
                else
                {
                    // Записываем ошибку с понятным описанием
                    string description = GetSmartErrorDescription(currentState, currentLexem);
                    AddError(currentLexem, description, currentLexem.lexemLine, currentLexem.lexemStartPosition);
                    stateLog.Add($"ОШИБКА: {description}");

                    errorFoundInCurrentStatement = true;
                    lastErrorPositionInLine = currentLexem.lexemStartPosition;

                    // Переходим к следующей лексеме
                    position++;

                    // ВАЖНО: Не сбрасываем состояние полностью, а пытаемся восстановиться
                    // в зависимости от текущего состояния
                    if (currentState == ParserState.IdRem)
                    {
                        // После ошибки в IdRem (ожидали =, получили что-то другое)
                        // Переходим в состояние Start, чтобы найти следующий оператор
                        currentState = ParserState.Start;
                        ResetFormatSpecifier();
                    }
                    else if (currentState == ParserState.AfterEqual)
                    {
                        // После ошибки в AfterEqual (ожидали кавычку)
                        // Переходим в состояние Start
                        currentState = ParserState.Start;
                        ResetFormatSpecifier();
                    }
                    else
                    {
                        // Для остальных состояний - сбрасываем в Start
                        currentState = ParserState.Start;
                        ResetFormatSpecifier();
                    }
                }
            }

            // Проверка на отсутствие ';' в конце
            if (currentState != ParserState.End && currentState != ParserState.Start)
            {
                if (!errorFoundInCurrentStatement)
                {
                    int lastLine = lexems.Count > 0 ? lexems.Last().lexemLine : currentLine;
                    int lastPos = lexems.Count > 0 ? lexems.Last().lexemEndPosition : 0;
                    AddError(null, "Отсутствует точка с запятой ';' в конце оператора", lastLine, lastPos);
                    stateLog.Add($"ОШИБКА: Отсутствует ';' в конце оператора");
                }
            }

            return errors.Count == 0;
        }

        private bool IsExpectedLexem(ParserState state, Lexem lexem)
        {
            switch (state)
            {
                case ParserState.Start:
                    return lexem.lexemCode == 1 ||   // идентификатор
                           lexem.lexemCode == 2 ||   // format
                           lexem.lexemCode == 18 ||  // ;
                           lexem.lexemCode == 16 ||  // (
                           lexem.lexemCode == 9;     // .
                case ParserState.IdRem:
                    return lexem.lexemCode == 4;
                case ParserState.AfterEqual:
                    return lexem.lexemCode == 5;
                case ParserState.OpenQuote:
                    return lexem.lexemCode == 21;
                case ParserState.InFormatSpecifier:
                    return IsValidForCurrentStep(lexem);
                case ParserState.CloseQuote:
                    return lexem.lexemCode == 5;
                case ParserState.AfterDot:
                    return lexem.lexemCode == 9;
                case ParserState.Format:
                    return lexem.lexemCode == 2;
                case ParserState.OpenArg:
                    return lexem.lexemCode == 16;
                case ParserState.InNumber:
                    return lexem.lexemCode == 7 || lexem.lexemCode == 8 ||
                           lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                           lexem.lexemCode == 14 || lexem.lexemCode == 15;
                case ParserState.AfterNumber:
                    return lexem.lexemCode == 17;
                case ParserState.CloseArg:
                    return lexem.lexemCode == 18;
                default:
                    return false;
            }
        }

        private bool IsValidForCurrentStep(Lexem lexem)
        {
            switch (formatSpecifierStep)
            {
                case 0: return lexem.lexemCode == 21; // {
                case 1: return lexem.lexemCode == 22; // :
                case 2: return lexem.lexemCode == 23; // f
                case 3: return lexem.lexemCode == 24; // }
                default: return false;
            }
        }

        private void ProcessFormatSpecifier(Lexem lexem)
        {
            switch (formatSpecifierStep)
            {
                case 0:
                    if (lexem.lexemCode == 21)
                    {
                        formatSpecifierStep = 1;
                        formatSpecifierStartPosition = lexem.lexemStartPosition;
                    }
                    else
                    {
                        formatSpecifierErrorReported = true;
                    }
                    break;
                case 1:
                    if (lexem.lexemCode == 22)
                    {
                        formatSpecifierStep = 2;
                    }
                    else
                    {
                        formatSpecifierErrorReported = true;
                    }
                    break;
                case 2:
                    if (lexem.lexemCode == 23)
                    {
                        formatSpecifierStep = 3;
                    }
                    else
                    {
                        formatSpecifierErrorReported = true;
                    }
                    break;
                case 3:
                    if (lexem.lexemCode == 24)
                    {
                        formatSpecifierStep = 0;
                    }
                    else
                    {
                        formatSpecifierErrorReported = true;
                    }
                    break;
            }
        }

        private void ProcessLexem(Lexem lexem)
        {
            stateLog.Add($"Состояние: {currentState}, Лексема: '{lexem.lexemContaintment}' (код: {lexem.lexemCode})");

            switch (currentState)
            {
                case ParserState.Start:
                    if (lexem.lexemCode == 1)
                    {
                        currentState = ParserState.IdRem;
                        errorFoundInCurrentStatement = false;
                    }
                    else if (lexem.lexemCode == 18)
                    {
                        currentState = ParserState.End;
                        stateLog.Add($"  -> Найдена ';', оператор завершён");
                    }
                    break;
                case ParserState.IdRem:
                    if (lexem.lexemCode == 4) currentState = ParserState.AfterEqual;
                    break;
                case ParserState.AfterEqual:
                    if (lexem.lexemCode == 5) currentState = ParserState.OpenQuote;
                    break;
                case ParserState.OpenQuote:
                    if (lexem.lexemCode == 21)
                    {
                        currentState = ParserState.InFormatSpecifier;
                        ResetFormatSpecifier();
                        ProcessFormatSpecifier(lexem);
                    }
                    break;
                case ParserState.InFormatSpecifier:
                    ProcessFormatSpecifier(lexem);

                    if (formatSpecifierStep == 0 && !formatSpecifierErrorReported)
                    {
                        currentState = ParserState.CloseQuote;
                        stateLog.Add($"  -> Форматный спецификатор успешно распознан");
                    }
                    else if (formatSpecifierErrorReported)
                    {
                        string description = "Неверный форматный спецификатор. Ожидался '{:f}'";
                        AddError(lexem, description, lexem.lexemLine, formatSpecifierStartPosition);
                        stateLog.Add($"ОШИБКА: {description}");

                        errorFoundInCurrentStatement = true;
                        skipToNextStatement = true; 
                        currentState = ParserState.Start;
                        ResetFormatSpecifier();
                    }
                    break;
                case ParserState.CloseQuote:
                    if (lexem.lexemCode == 5)
                    {
                        currentState = ParserState.AfterDot;
                        stateLog.Add($"  -> Найдена закрывающая кавычка");
                    }
                    break;
                case ParserState.AfterDot:
                    if (lexem.lexemCode == 9)
                    {
                        currentState = ParserState.Format;
                        stateLog.Add($"  -> Найдена точка");
                    }
                    break;
                case ParserState.Format:
                    if (lexem.lexemCode == 2)
                    {
                        currentState = ParserState.OpenArg;
                        stateLog.Add($"  -> Найдено format");
                    }
                    break;
                case ParserState.OpenArg:
                    if (lexem.lexemCode == 16)
                    {
                        currentState = ParserState.InNumber;
                        stateLog.Add($"  -> Найдена (");
                    }
                    break;
                case ParserState.InNumber:
                    if (lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                        lexem.lexemCode == 14 || lexem.lexemCode == 15)
                    {
                        hasNumberInCurrentState = true;
                        currentState = ParserState.AfterNumber;
                        stateLog.Add($"  -> Найдено число");
                    }
                    else if (lexem.lexemCode == 7 || lexem.lexemCode == 8)
                    {
                        currentState = ParserState.InNumber;
                        stateLog.Add($"  -> Найден знак числа");
                    }
                    break;
                case ParserState.AfterNumber:
                    if (lexem.lexemCode == 17)
                    {
                        currentState = ParserState.CloseArg;
                        stateLog.Add($"  -> Найдена )");
                    }
                    break;
                case ParserState.CloseArg:
                    if (lexem.lexemCode == 18)
                    {
                        currentState = ParserState.End;
                        stateLog.Add($"  -> Найдена ;");
                    }
                    break;
            }
        }

        private string GetSmartErrorDescription(ParserState state, Lexem lexem)
        {
            // Обработка лексем ошибок
            if (lexem.lexemCode == 19)
            {
                if (lexem.lexemContaintment == "{:f}" || lexem.lexemContaintment == "{:g}" ||
                    lexem.lexemContaintment == "{-f}" || lexem.lexemContaintment.Contains(":"))
                {
                    if (state == ParserState.IdRem)
                        return $"Отсутствует '=' перед форматным спецификатором '{lexem.lexemContaintment}'";
                    if (state == ParserState.AfterEqual)
                        return $"Отсутствует открывающая кавычка перед форматным спецификатором '{lexem.lexemContaintment}'";
                    return $"Неверный форматный спецификатор '{lexem.lexemContaintment}'. Ожидался '{{:f}}'";
                }
                return $"Недопустимый символ '{lexem.lexemContaintment}'";
            }

            // Для состояния Start
            if (state == ParserState.Start)
            {
                switch (lexem.lexemCode)
                {
                    case 5: return "Отсутствует идентификатор перед строкой";
                    case 9: return "Отсутствует идентификатор перед точкой";
                    case 2: return "Отсутствует точка перед 'format'";
                    case 16: return "Отсутствует 'format' перед '('";
                    case 12:
                    case 13:
                    case 14:
                    case 15: return "Отсутствует '(' перед числом";
                    case 17: return "Отсутствует число перед ')'";
                    case 18: return "Отсутствует выражение перед ';'";
                    default: return $"Ожидался идентификатор, получен '{lexem.lexemContaintment}'";
                }
            }

            // Для состояния IdRem
            if (state == ParserState.IdRem)
            {
                return $"Ожидался '=', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния AfterEqual
            if (state == ParserState.AfterEqual)
            {
                if (lexem.lexemCode == 21)
                    return "Отсутствует открывающая кавычка перед форматным спецификатором";
                return $"Ожидалась открывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния OpenQuote
            if (state == ParserState.OpenQuote)
            {
                if (lexem.lexemCode == 5)
                    return "Отсутствует форматный спецификатор между кавычками";
                return $"Ожидалась открывающая фигурная скобка '{{', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния CloseQuote
            if (state == ParserState.CloseQuote)
            {
                if (lexem.lexemCode == 2)
                    return "Отсутствует точка перед 'format'";
                if (lexem.lexemCode == 9)
                    return "Ожидалась точка, но после кавычки должен быть метод format";
                return $"Ожидалась закрывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния AfterDot
            if (state == ParserState.AfterDot)
            {
                if (lexem.lexemCode != 2)
                    return $"Ожидалось ключевое слово 'format', получено '{lexem.lexemContaintment}'";
            }

            // Для состояния Format
            if (state == ParserState.Format)
            {
                if (lexem.lexemCode != 16)
                    return $"Ожидалась открывающая скобка '(', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния OpenArg
            if (state == ParserState.OpenArg)
            {
                if (lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                    lexem.lexemCode == 14 || lexem.lexemCode == 15)
                    return null; // нет ошибки
                return $"Ожидалось число, получен '{lexem.lexemContaintment}'";
            }

            // Для состояния AfterNumber
            if (state == ParserState.AfterNumber)
            {
                if (lexem.lexemCode != 17)
                    return $"Ожидалась закрывающая скобка ')', получен '{lexem.lexemContaintment}'";
            }

            // Для состояния CloseArg
            if (state == ParserState.CloseArg)
            {
                if (lexem.lexemCode != 18)
                    return $"Ожидалась точка с запятой ';', получен '{lexem.lexemContaintment}'";
            }

            return GetErrorDescription(state, lexem);
        }
        private string GetErrorDescription(ParserState state, Lexem lexem)
        {
            if (lexem.lexemCode == 19)
            {
                return $"Недопустимый символ '{lexem.lexemContaintment}'";
            }

            // Специальная диагностика для состояния Start
            if (state == ParserState.Start)
            {
                switch (lexem.lexemCode)
                {
                    case 5: return "Отсутствует идентификатор перед строкой";
                    case 9: return "Отсутствует идентификатор перед точкой";
                    case 2: return "Отсутствует точка перед 'format'";
                    case 16: return "Отсутствует 'format' перед '('";
                    case 12:
                    case 13:
                    case 14:
                    case 15: return "Отсутствует '(' перед числом";
                    case 17: return "Отсутствует число перед ')'";
                    case 18: return "Отсутствует выражение перед ';'";
                    default: return $"Ожидался идентификатор, получен '{lexem.lexemContaintment}'";
                }
            }

            if (state == ParserState.CloseQuote && lexem.lexemCode != 5)
            {
                if (lexem.lexemContaintment == "f" || lexem.lexemContaintment == "fo" ||
                    lexem.lexemContaintment == "for" || lexem.lexemContaintment == "form" ||
                    lexem.lexemContaintment == "forma" || lexem.lexemContaintment == "format")
                {
                    return "Отсутствует закрывающая кавычка и точка перед format";
                }
                return $"Отсутствует закрывающая кавычка перед '{lexem.lexemContaintment}'";
            }

            switch (state)
            {
                case ParserState.IdRem:
                    return $"Ожидался '=', получен '{lexem.lexemContaintment}'";
                case ParserState.AfterEqual:
                    if (lexem.lexemCode == 21)
                        return "Отсутствует открывающая кавычка перед форматным спецификатором";
                    return $"Ожидалась открывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
                case ParserState.OpenQuote:
                    return $"Ожидалась открывающая фигурная скобка '{{', получен '{lexem.lexemContaintment}'";
                case ParserState.InFormatSpecifier:
                    return GetFormatSpecifierError(lexem);
                case ParserState.CloseQuote:
                    return $"Ожидалась закрывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
                case ParserState.AfterDot:
                    return $"Ожидалась точка '.', получен '{lexem.lexemContaintment}'";
                case ParserState.Format:
                    return $"Ожидалось ключевое слово 'format', получено '{lexem.lexemContaintment}'";
                case ParserState.OpenArg:
                    return $"Ожидалась открывающая скобка '(', получен '{lexem.lexemContaintment}'";
                case ParserState.InNumber:
                    return $"Ожидалось число или знак числа, получен '{lexem.lexemContaintment}'";
                case ParserState.AfterNumber:
                    return $"Ожидалась закрывающая скобка ')', получен '{lexem.lexemContaintment}'";
                case ParserState.CloseArg:
                    return $"Ожидалась точка с запятой ';', получен '{lexem.lexemContaintment}'";
                default:
                    return $"Синтаксическая ошибка, получен '{lexem.lexemContaintment}'";
            }
        }

        private string GetFormatSpecifierError(Lexem lexem)
        {
            return $"Неверный форматный спецификатор. Ожидался '{{:f}}'";
        }

        private void AddError(Lexem lexem, string description, int line, int position)
        {
            if (errors.Any(e => e.Line == line && e.Position == position))
                return;

            errors.Add(new SyntaxError
            {
                Fragment = lexem?.lexemContaintment ?? "конец ввода",
                Line = line,
                Position = position,
                Description = description
            });
        }

        public List<SyntaxError> GetErrors()
        {
            return errors;
        }

        public void PrintLog(StringBuilder sb)
        {
            sb.AppendLine("  Лог состояний конечного автомата:");
            foreach (var entry in stateLog)
            {
                sb.AppendLine($"    {entry}");
            }
        }

        public List<string> GetStateLog()
        {
            return stateLog;
        }
    }
}