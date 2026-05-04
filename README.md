# Лабораторная работа 5: Построение AST и проверка контекстно-зависимых условий

**Автор:** Соболев Илья Олегович

---

## Вариант задания

**Тема работы:** Форматирование научной нотации в число с плавающей точкой на языке Python.

**Формат строки:**
```python
float_format = "{:f}".format(3.234e+4);

Примеры верных строк:
float = "{:f}".format(1e+4);

float_format = "{:f}".format(3.234e+4);

result = "{:f}".format(3.234555e+4);

Контекстно-зависимые условия

В лабораторной работе реализованы следующие проверки контекстно-зависимых условий:

| № | Проверка | Описание | Пример ошибки | Ожидаемое сообщение |
|---|----------|----------|---------------|---------------------|
| 1 | Уникальность имён | Запрет повторного объявления переменной с тем же именем в одной области видимости | `float x = 1; float x = 2;` | `"Variable 'x' already declared"` |
| 2 | Совместимость типов | Проверка соответствия типов при присваивании и передаче аргументов | `int x = "{:f}".format(3e+4);` | `"Type mismatch: expected String, got FormatCallNode"` |
| 3 | Допустимые значения | Проверка корректности формата научной нотации (мантисса, экспонента) | `float_format = "{:f}".format(1e+999);` | `"Exponent value out of valid range"` |
| 4 | Использование объявленных идентификаторов | Запрет использования переменных до их объявления | `result = format(x);` (если x не объявлен) | `"Variable 'x' is not declared"` |
| 5 | Корректный specifier форматирования | Проверка, что specifier является допустимым для чисел с плавающей точкой | `float_format = "{:d}".format(3.14e+4);` | `"Invalid format specifier for float: expected '{:f}'"` |
| 6 | Наличие обязательных компонентов | Проверка наличия и точки, и форматной строки, и аргумента | `float_format = .format(3e+4);` | `"Missing required format string component"` |

Структура AST для верной строки

| Уровень | Тип узла | Поле | Значение |
|---------|----------|------|----------|
| 0 | `ProgramNode` | statements | `[AssignmentNode]` |
| 1 | `AssignmentNode` | name | `"float_format"` |
| 1 | `AssignmentNode` | type | `StringNode` |
| 1 | `AssignmentNode` | value | `FormatCallNode` |
| 2 | `FormatCallNode` | specifier | `"{:f}"` |
| 2 | `FormatCallNode` | argument | `ScientificNumberNode` |
| 3 | `ScientificNumberNode` | mantissa | `3.234` |
| 3 | `ScientificNumberNode` | exponent | `4` |
| 3 | `ScientificNumberNode` | sign | `+` |

Диаграмма CST
![Дерево CST](screenshots/CST_дерево.png)

Формат вывода AST в программе

ProgramNode
├── AssignmentNode
│   ├── name: "float_format"
│   ├── type: StringNode
│   └── value: FormatCallNode
│       ├── specifier: "\"{:f}\""
│       └── argument: ScientificNumberNode
│           └── value: 3.234e+4

Тестовые примеры:

![Тестовый пример верный](screenshots/Пример_верный.png)

![Тестовый пример неверный](screenshots/Пример_неверный1.png)

![Тестовый пример неверный](screenshots/Пример_неверный2.png)
