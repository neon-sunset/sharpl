﻿using System.Diagnostics;
using Sharpl;
using Ops = Sharpl.Ops;

var vm = new VM();
vm.UserLib.Import(vm.CoreLib);
vm.UserLib.Import(vm.TermLib);
var Int = new Type<int>("Int");
var v = Value.Make(Int, 42);
vm.Emit(Ops.Push.Make(v));
vm.Emit(Ops.Stop.Make());
var stack = new Stack(32);
vm.Eval(0, stack);
Debug.Assert(stack.Peek() == v);
vm.REPL();