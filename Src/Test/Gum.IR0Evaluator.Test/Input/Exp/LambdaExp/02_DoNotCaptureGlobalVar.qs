﻿// 4

// global은 복사 캡쳐하지 않는다 (모듈 외부 노출이던 그냥 내부에서만 쓰는 글로벌이던)
int x = 3;

var f = () => { @{$x} };

x = 4;

f(); 