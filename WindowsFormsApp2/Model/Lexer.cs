using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2.Model
{
    public class Lexer
    {
        private string text;
        private int position;
        private int currentLine;
        private List<Lexem> lexemsList;

        public Lexer(string textToScan)
        {
            text = textToScan ?? "";
            position = 0;
            currentLine = 1;
            lexemsList = new List<Lexem>();
        }

        public List<Lexem> Scan()
        {
            while (position < text.Length)
            {
                char currentChar = text[position];

                if (currentChar == '\n')
                {
                    currentLine++;
                    position++;
                    continue;
                }

                if (currentChar == ' ' || currentChar == '\t' || currentChar == '\r')
                {
                    position++;
                    continue;
                }

                if (char.IsLetter(currentChar) || currentChar == '_')
                {
                    ProcessIdentifier();
                }
                else if (char.IsDigit(currentChar) || currentChar == '+' || currentChar == '-')
                {
                    ProcessNumber();
                }
                else
                {
                    if (ProcessSymbol()) break;
                }
            }
            return lexemsList;
        }

        private void ProcessIdentifier()
        {
            int start = position;
            int line = currentLine;

            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
            {
                position++;
            }

            string lexeme = text.Substring(start, position - start);

            if (lexeme == "format")
            {
                lexemsList.Add(new Lexem(2, lexeme, start, position, line));
            }
            else
            {
                lexemsList.Add(new Lexem(1, lexeme, start, position, line));
            }
        }

        private void ProcessNumber()
        {
            int start = position;
            int line = currentLine;
            bool hasDot = false;
            bool hasExp = false;
            bool valid = true;

            if (text[position] == '+' || text[position] == '-')
            {
                position++;
            }

            while (position < text.Length && char.IsDigit(text[position]))
            {
                position++;
            }

            if (position < text.Length && text[position] == '.')
            {
                hasDot = true;
                position++;

                while (position < text.Length && char.IsDigit(text[position]))
                {
                    position++;
                }
            }

            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                hasExp = true;
                position++;

                if (position < text.Length && (text[position] == '+' || text[position] == '-'))
                {
                    position++;
                }

                if (position < text.Length && char.IsDigit(text[position]))
                {
                    while (position < text.Length && char.IsDigit(text[position]))
                    {
                        position++;
                    }
                }
                else
                {
                    valid = false;
                }
            }

            string lexeme = text.Substring(start, position - start);

            if (!valid)
            {
                lexemsList.Add(new Lexem(19, lexeme, start, position, line));
            }
            else if (hasExp)
            {
                if (lexeme.Contains('-') && lexeme.IndexOf('-') > 0)
                    lexemsList.Add(new Lexem(14, lexeme, start, position, line));
                else if (lexeme.Contains('+') && lexeme.IndexOf('+') > 0)
                    lexemsList.Add(new Lexem(15, lexeme, start, position, line));
                else
                    lexemsList.Add(new Lexem(13, lexeme, start, position, line));
            }
            else if (hasDot)
            {
                lexemsList.Add(new Lexem(13, lexeme, start, position, line));
            }
            else
            {
                lexemsList.Add(new Lexem(12, lexeme, start, position, line));
            }
        }

        private bool ProcessSymbol()
        {
            int start = position;
            int line = currentLine;
            char currentChar = text[position++];
            int code;

            switch (currentChar)
            {
                case '=':
                    code = 4;
                    break;
                case '"':
                    ProcessString();
                    return false;
                case '-':
                    code = 7;
                    break;
                case '+':
                    code = 8;
                    break;
                case '.':
                    code = 9;
                    break;
                case '(':
                    code = 16;
                    break;
                case ')':
                    code = 17;
                    break;
                case ';':
                    code = 18;
                    break;
                case '{':
                    // Проверяем, не является ли это началом правильного форматного спецификатора {:f}
                    if (position + 2 < text.Length &&
                        text[position] == ':' &&
                        text[position + 1] == 'f' &&
                        text[position + 2] == '}')
                    {
                        // Это {:f} - создаем одну лексему ошибки (потому что вне строки)
                        string errorFragment = "{:f}";
                        lexemsList.Add(new Lexem(19, errorFragment, start, position + 3, line));
                        position += 3;
                        return false;
                    }
                    // Одиночная открывающая скобка - ошибка
                    code = 19;
                    lexemsList.Add(new Lexem(code, currentChar.ToString(), start, position, line));
                    return false;
                case ':':
                    // Проверяем, не является ли это началом неправильного спецификатора :f или :f}
                    if (position < text.Length && text[position] == 'f')
                    {
                        if (position + 1 < text.Length && text[position + 1] == '}')
                        {
                            // Последовательность :f}
                            string errorFragment = ":f}";
                            lexemsList.Add(new Lexem(19, errorFragment, start, position + 2, line));
                            position += 3;
                            return false;
                        }
                        else
                        {
                            // Только :f
                            string errorFragment = ":f";
                            lexemsList.Add(new Lexem(19, errorFragment, start, position + 1, line));
                            position += 2;
                            return false;
                        }
                    }
                    // Обычное двоеточие вне строки - ошибка
                    lexemsList.Add(new Lexem(19, currentChar.ToString(), start, position, line));
                    return false;
                case '}':
                    // Одиночная закрывающая скобка вне строки - ошибка
                    lexemsList.Add(new Lexem(19, currentChar.ToString(), start, position, line));
                    return false;
                default:
                    code = 19;
                    break;
            }

            lexemsList.Add(new Lexem(code, currentChar.ToString(), start, position, line));
            return false;
        }

        private void ProcessString()
        {
            int startQuotePos = position - 1; // позиция открывающей кавычки
            int line = currentLine;

            // Добавляем открывающую кавычку
            lexemsList.Add(new Lexem(5, "\"", startQuotePos, position, line));

            // Ищем закрывающую кавычку
            int closingQuotePos = -1;
            int tempPos = position;
            while (tempPos < text.Length)
            {
                char ch = text[tempPos];
                if (ch == '"')
                {
                    closingQuotePos = tempPos;
                    break;
                }
                if (ch == '\n')
                {
                    break; // перевод строки до кавычки — ошибка
                }
                tempPos++;
            }

            if (closingQuotePos != -1)
            {
                // Есть закрывающая кавычка — обрабатываем содержимое строки
                while (position < closingQuotePos)
                {
                    char currentChar = text[position];
                    // Распознаём {:f} как специальную конструкцию
                    if (currentChar == '{' && position + 3 < closingQuotePos &&
                        text[position + 1] == ':' &&
                        text[position + 2] == 'f' &&
                        text[position + 3] == '}')
                    {
                        lexemsList.Add(new Lexem(21, "{", position, position + 1, line));
                        position++;
                        lexemsList.Add(new Lexem(22, ":", position, position + 1, line));
                        position++;
                        lexemsList.Add(new Lexem(23, "f", position, position + 1, line));
                        position++;
                        lexemsList.Add(new Lexem(24, "}", position, position + 1, line));
                        position++;
                    }
                    else
                    {
                        // Обычный символ внутри строки
                        lexemsList.Add(new Lexem(3, currentChar.ToString(), position, position + 1, line));
                        position++;
                    }
                }
                // Пропускаем закрывающую кавычку
                position++; // перешагиваем "
                lexemsList.Add(new Lexem(5, "\"", closingQuotePos, closingQuotePos + 1, line));
            }
            else
            {
                // Нет закрывающей кавычки — выдаём ОДНУ ошибку и пропускаем все символы до ';' или конца строки
                while (position < text.Length && text[position] != ';' && text[position] != '\n')
                {
                    position++;
                }
                // Добавляем одну лексему ошибки
                lexemsList.Add(new Lexem(19, "Незакрытая строка (отсутствует закрывающая кавычка)", startQuotePos, position, line));
                // Если остановились на ';' — не поглощаем его, он будет обработан позже как отдельная лексема
            }
        }
    }
}