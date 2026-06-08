# 语法高亮测试

## C# 代码
```csharp
class Program {
    static void Main(string[] args) {
        Console.WriteLine("Hello, World!");
        int[] numbers = { 1, 2, 3, 4, 5 };
        foreach (int n in numbers) {
            Console.WriteLine(n * n);
        }
    }
}
```

## Python 代码
```python
def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

for i in range(10):
    print(fibonacci(i))
```

## JavaScript 代码
```javascript
function greet(name) {
    const message = `Hello, ${name}!`;
    console.log(message);
    return message;
}

const users = ["Alice", "Bob", "Charlie"];
users.forEach(user => greet(user));
```

## Java 代码
```java
public class HelloWorld {
    public static void main(String[] args) {
        String[] languages = {"C#", "Python", "JavaScript"};
        for (String lang : languages) {
            System.out.println("Language: " + lang);
        }
    }
}
```

## SQL 代码
```sql
SELECT users.name, orders.total
FROM users
INNER JOIN orders ON users.id = orders.user_id
WHERE orders.total > 100
ORDER BY orders.total DESC;
```

## 普通代码块（无语言标记）
```
This is plain text without language specification.
It should still display correctly.
```
