# 문자열 생성

따옴표를 사용하여 문자열을 생성할 수 있습니다. 

문자열 표현 중간에 $을 사용해서 식을 쓸수 있습니다. 문자로만 이뤄진 변수 등은 $뒤에 바로 문자를 붙이면 되고, 식 뒤에 바로 텍스트를 붙여야 한다던가, 문자가 아닌 복잡한 식 표현을 쓸 땐 ${식} 처럼 중괄호로 묶어줍니다. 

문자열 중간에 오는 식의 타입은 꼭 string이 아니더라도 괜찮습니다. int, bool 등 string으로 변환 가능한 타입이면 가능합니다.

$를 입력하고 싶으면 $$와 같이 두번 씁니다.

```csharp
var greeting = "hi";
var n = 16;

var text = "$greeting, ${n + 1} $$"; // hi, 17 $
```