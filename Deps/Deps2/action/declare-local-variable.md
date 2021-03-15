# 지역 변수 정의

함수 본문 또는 [[top-level-statement|최상위 Statement]] 에서 지역 변수를 선언할 수 있습니다. 지역 변수는 스코프에 의존하며, 스코프가 사라지는 경우 값도 더 이상 참조할 수 없게 되어 사라집니다.

지역 변수는 선언과 동시에 초기화 될 수 있습니다

```csharp
void func()
{
    int a = 0; // 지역 변수 선언
}
```

함수 인자도 지역 변수의 일종으로 볼 수 있습니다.
void func(int a)
{

}