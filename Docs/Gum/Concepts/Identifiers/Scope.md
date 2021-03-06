# 범위 Scope

범위는 타입, 함수, 변수의 이름을 어디까지 노출할지를 결정하는 요소입니다. 또한, 범위는 선언된 지역 변수의 소멸시점을 결정하는 요소입니다.

전역 범위와 지역 범위로 나눌 수 있습니다. 전역 범위는 소스의 모든 영역에서 접근 가능함을 말하며, 지역 범위는 선언된 범위를 벗어나면 선언한 이름을 사용할 수 없습니다

모든 함수, 타입은 전역 범위를 가지며, 변수는 전역 범위 변수와 지역 범위 변수가 각각 존재합니다. (이를 간단히 전역 변수, 지역 변수라고 부릅니다)

```csharp
// 전역 타입, 
// 클래스 타입 'C'는 덮어씌워지지 않는 한 소스 어디서나 이 타입을 지칭하는 데 사용할 수 있습니다.
class C { }
class D { public class E { } } // 중첩 타입도 전역 범위입니다

// 전역 함수,
// 'F' 이름은 덮어씌워지지 않는 한 소스 어디서나 이 함수를 지칭하는데 사용할 수 있습니다.
int F() { return 3; }

// 전역 변수, 
string x = "Hello";  

void Main()
{
    var c = new C();      // C를 Main안에서 사용했습니다
    var e = new D.E();    // D.E를 Main안에서 사용했습니다

    F();                  // Main 함수 범위 안에서도 'F' 이름을 사용 할 수 있습니다
    Assert(x == "Hello"); // Main 함수 범위 안에서 전역 x를 사용했습니다

    {
        int x = 2;        // 이제 x는 int 타입 지역 변수입니다
        Assert(x == 2);   // 
    }

    x = "World";          // 지역 변수 x는 범위를 벗어났으므로 다시 전역 변수를 가리킵니다.
}
```

# 지역 변수가 가리키는 값의 소멸시점

지역 변수의 범위가 끝나면, 해당 이름으로 참조할 수 없을 뿐만 아니라, 가리키는 변수 값의 소멸이 보장됩니다. 따라서, 지역 변수가 가리키는 값을 보다 넓은 범위에서 선언된 변수가 가리키게 할 수 없습니다. 이 과정은 언뜻 당연해 보이지만, Lambda, Task, Async, out 등을 사용하게 되면 지역 범위가 끝난 이후 시점에도 변수 참조를 수행할 수 있기 때문에 중요합니다. 컴파일러는 정적 검사를 통해 변수 참조시 컴파일 에러를 냅니다.

```csharp
type F = void => int;

F MakeFunc()
{
	int x = 1;
	return () => { x = 3; }; // 에러, Lambda 내부에서 지역 변수가 가리키는 값에 대입하려고 했습니다. x를 명시적으로 heap에 할당해야 합니다.
}

var f = MakeFunc();
f(); // 에러가 날 지점
```