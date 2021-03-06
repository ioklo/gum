# Struct

# 선언

```csharp
// accessor STRUCT ID<ID...> (COLON TypeExp (COMMA TypeExp)...)
public struct S<T...> : B, I...
{
    // member variable 
    // Accessor VarType Id1, ...;
    T x; 
    private T y;

    // member functions, virtual not allowed
    // [accessor] [static] [seq] ReturnType Id<TypeArgs...>(Args...) { Body }
    public static seq T Func<U>(U u) { return y; }
}
```

# 생성

```csharp
var s1 = new S<int>(1, 2); // S<int> type, scoped
var s2 = box S<int>(3, 4); // S<int>* type, garbage collectable
```

# 멤버 변수 읽기, 쓰기

```csharp
var s1 = new S<int>(1, 2);
Assert(s1.x == 1);

var s2 = box S<int>(3, 4);
Assert(s2.x == 3);
```

# 참조, 복사

```csharp
var s1 = new S<int>(1, 2); // S<int> type, scoped
var s2 = box S<int>(3, 4); // S<int>* type, garbage collectable

var s3 = s1;     // S<int> 타입, 복사
var s4 = box s1; // S<int>* 타입, 복사
var s5 = s2;     // gc reference

S<int>& s6 = &s3;    // scoped reference, var& s6 = s3;
S<int>* s7 = s5;     // scoped reference can reference gc reference
// S<int>* s8 = s7;        // not allowed

s3.x = 6;
Assert(s1.x == 1 && s3.x == 6);

s4.x = 7;
Assert(s1.x == 1 && s4.x == 7);

s5.x = 8;
Assert(s2.x == 8 && s5.x == 8);

s6.x = 9;
Assert(s3.x == 9 && s6.x == 9);
```

# 캐스팅

```csharp
// can cast only reference
var s1 = new S<int>(1, 2);
var b1 = new B();
b1 = s1;    // slicing not allowed, 
B& b2 = s1; 
if (b2 is S<int>&) // downcast, b2 is now S& for read, B& for write, for a while
{
    if (rand() % 2 == 0)
        *b2 = new S<int>(7, 8);  // b2(S<int>& for read), *b2 (S<int>)

    // after write b2(even conditional), b2 is B&
}

// S<int>* type
var s3 = box S<int>(1, 2);
B& b3 = s3;
B* b4 = s3;

if (b4 is S<int>*) // downcast, b2 is now S& for read, B& for write, for a while
{

}

// interface casting, gc reference
S* s5 = box S<int>(2, 3);
I i5 = s5;

// Object casting, gc reference
S* s6 = box S<int>(2, 3);
Object o6 = s6;
```