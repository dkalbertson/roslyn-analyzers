﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using Test.Utilities;
using Xunit;
using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VisualBasicLanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;

namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.DisposeAnalysis)]
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.PointsToAnalysis)]
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.NullAnalysis)]
    public partial class DisposeObjectsBeforeLosingScopeTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() => new DisposeObjectsBeforeLosingScope();
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DisposeObjectsBeforeLosingScope();

        private DiagnosticResult GetCSharpResultAt(int line, int column, string allocationText) =>
            GetCSharpResultAt(line, column, DisposeObjectsBeforeLosingScope.NotDisposedRule, allocationText);
        private DiagnosticResult GetCSharpMayBeNotDisposedResultAt(int line, int column, string allocationText) =>
            GetCSharpResultAt(line, column, DisposeObjectsBeforeLosingScope.MayBeDisposedRule, allocationText);
        private DiagnosticResult GetCSharpNotDisposedOnExceptionPathsResultAt(int line, int column, string allocationText) =>
            GetCSharpResultAt(line, column, DisposeObjectsBeforeLosingScope.NotDisposedOnExceptionPathsRule, allocationText);
        private DiagnosticResult GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(int line, int column, string allocationText) =>
            GetCSharpResultAt(line, column, DisposeObjectsBeforeLosingScope.MayBeDisposedOnExceptionPathsRule, allocationText);

        private DiagnosticResult GetBasicResultAt(int line, int column, string allocationText) =>
            GetBasicResultAt(line, column, DisposeObjectsBeforeLosingScope.NotDisposedRule, allocationText);
        private DiagnosticResult GetBasicMayBeNotDisposedResultAt(int line, int column, string allocationText) =>
            GetBasicResultAt(line, column, DisposeObjectsBeforeLosingScope.MayBeDisposedRule, allocationText);
        private DiagnosticResult GetBasicNotDisposedOnExceptionPathsResultAt(int line, int column, string allocationText) =>
            GetBasicResultAt(line, column, DisposeObjectsBeforeLosingScope.NotDisposedOnExceptionPathsRule, allocationText);
        private DiagnosticResult GetBasicMayBeNotDisposedOnExceptionPathsResultAt(int line, int column, string allocationText) =>
            GetBasicResultAt(line, column, DisposeObjectsBeforeLosingScope.MayBeDisposedOnExceptionPathsRule, allocationText);

        private FileAndSource GetEditorConfigFileToDisableInterproceduralAnalysis(DisposeAnalysisKind disposeAnalysisKind)
        {
            var text = $@"dotnet_code_quality.interprocedural_analysis_kind = None
                          dotnet_code_quality.dispose_analysis_kind = {disposeAnalysisKind.ToString()}";
            return GetEditorConfigAdditionalFile(text);
        }

        private FileAndSource GetEditorConfigFile(DisposeAnalysisKind disposeAnalysisKind)
            => GetEditorConfigAdditionalFile($@"dotnet_code_quality.dispose_analysis_kind = {disposeAnalysisKind.ToString()}");

        [Fact]
        public void LocalWithDisposableInitializer_DisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = new A();
        a.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As New A()
        a.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void LocalWithDisposableInitializer_NoDisposeCall_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = new A();
    }
}
",
            // Test0.cs(15,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(15, 17, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As New A()
    End Sub
End Class",
            // Test0.vb(12,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(12, 18, "New A()"));
        }

        [Fact]
        public void LocalWithDisposableAssignment_DisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();
        a.Dispose();

        A b = new A();
        a = b;
        a.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = New A()
        a.Dispose()

        Dim b As New A()
        a = b
        a.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void LocalWithDisposableAssignment_NoDisposeCall_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();
    }
}
",
            // Test0.cs(16,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(16, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = New A()
    End Sub
End Class",
            // Test0.vb(13,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 13, "New A()"));
        }

        [Fact]
        public void ParameterWithDisposableAssignment_DisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        a = new A();
        a.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        a = New A()
        a.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void ParameterWithDisposableAssignment_NoDisposeCall_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        a = new A();
    }
}
",
            // Test0.cs(15,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(15, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        a = New A()
    End Sub
End Class",
            // Test0.vb(12,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(12, 13, "New A()"));
        }

        [Fact]
        public void OutAndRefParametersWithDisposableAssignment_NoDisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(ref A a1, out A a2)
    {
        a1 = new A();
        a2 = new A();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(ByRef a As A)
        a = New A()
    End Sub
End Class");
        }

        [Fact]
        public void OutDisposableArgument_NoDisposeCall_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(out A param)
    {
        param = new A();
    }

    void M2(out A param2)
    {
        M3(out param2);
    }

    void M3(out A param3)
    {
        param3 = new A();
    }

    void Method()
    {
        A a;
        M1(out a);
        A local = a;
        M1(out a);

        M1(out var a2);

        A a3;
        M2(out a3);
    }
}
",
            // Test0.cs(32,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a' before all references to it are out of scope.
            GetCSharpResultAt(32, 12, "out a"),
            // Test0.cs(34,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a' before all references to it are out of scope.
            GetCSharpResultAt(34, 12, "out a"),
            // Test0.cs(36,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'out var a2' before all references to it are out of scope.
            GetCSharpResultAt(36, 12, "out var a2"),
            // Test0.cs(39,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a3' before all references to it are out of scope.
            GetCSharpResultAt(39, 12, "out a3"));
        }

        [Fact]
        public void OutDisposableArgument_DisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(out A param)
    {
        param = new A();
    }

    void M2(out A param2)
    {
        M3(out param2);
    }

    void M3(out A param3)
    {
        param3 = new A();
    }

    void Method()
    {
        A a;
        M1(out a);
        A local = a;
        M1(out a);

        M1(out var a2);

        A a3;
        M2(out a3);

        local.Dispose();
        a.Dispose();
        a2.Dispose();
        a3.Dispose();
    }
}
");
        }

        [Fact]
        public void TryGetSpecialCase_OutDisposableArgument_NoDisposeCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class MyCollection
{
    private readonly Dictionary<int, A> _map;
    public MyCollection(Dictionary<int, A> map)
    {
        _map = map;
    }

    public bool ValueExists(int i)
    {
        return _map.TryGetValue(i, out var value);
    }
}
");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public void OutDisposableArgument_StoredIntoField_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    private A _a;
    void M(out A param)
    {
        param = new A();
    }

    void Method()
    {
        M(out _a);  // This is considered as an escape of interprocedural disposable creation.
    }
}
");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public void OutDisposableArgument_WithinTryXXXInvocation_DisposedOnSuccessPath_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Concurrent;

public class C
{
    private readonly ConcurrentDictionary<object, IDisposable> _dictionary;
    public C(ConcurrentDictionary<object, IDisposable> dictionary)
    {
        _dictionary = dictionary;
    }

    public void Remove1(object key)
    {
        if (_dictionary.TryRemove(key, out IDisposable value))
        {
            value.Dispose();
        }
    }

    public void Remove2(object key)
    {
        if (!_dictionary.TryRemove(key, out IDisposable value))
        {
            return;
        }

        value.Dispose();
    }
}");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public void OutDisposableArgument_WithinTryXXXInvocation_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Concurrent;

public class C
{
    private readonly ConcurrentDictionary<object, IDisposable> _dictionary;
    public C(ConcurrentDictionary<object, IDisposable> dictionary)
    {
        _dictionary = dictionary;
    }

    public void Remove(object key)
    {
        if (_dictionary.TryRemove(key, out IDisposable value))
        {
            // value is not disposed.
        }
    }
}",
            // Test0.cs(15,40): warning CA2000: Call System.IDisposable.Dispose on object created by 'out IDisposable value' before all references to it are out of scope.
            GetCSharpResultAt(15, 40, "out IDisposable value"));
        }

        [Fact]
        public void LocalWithMultipleDisposableAssignment_DisposeCallOnSome_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();
        a = new A();
        a.Dispose();
        a = new A();
    }
}
",
            // Test0.cs(16,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(16, 13, "new A()"),
            // Test0.cs(19,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(19, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = New A()
        a = New A()
        a.Dispose()
        a = New A()
    End Sub
End Class",
            // Test0.vb(13,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 13, "New A()"),
            // Test0.vb(16,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(16, 13, "New A()"));
        }

        [Fact]
        public void FieldWithDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    public A a;
    void M1(Test p)
    {
        p.a = new A();

        Test l = new Test();
        l.a = new A();

        this.a = new A();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public a As A
    Sub M1(p As Test)
        p.a = New A()

        Dim l As New Test()
        l.a = New A()

        Me.a = New A()
    End Sub
End Class");
        }

        [Fact]
        public void PropertyWithDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    public A a { get; set; }
    void M1(Test p)
    {
        p.a = new A();

        Test l = new Test();
        l.a = new A();

        this.a = new A();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(p As Test)
        p.a = New A()

        Dim l As New Test()
        l.a = New A()

        Me.a = New A()
    End Sub
End Class");
        }

        [Fact]
        public void Interprocedural_DisposedInHelper_MethodInvocation_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1(Test2 t2)
    {
        DisposeHelper(new A());
        t2.DisposeHelper_MethodOnDifferentType(new A());
        DisposeHelper_MultiLevelDown(new A());
    }

    void DisposeHelper(A a)
    {
        a.Dispose();
    }

    void DisposeHelper_MultiLevelDown(A a)
    {
        DisposeHelper(a);
    }
}

class Test2
{
    public A a;
    public void DisposeHelper_MethodOnDifferentType(A a)
    {
        a.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(t2 As Test2)
        DisposeHelper(new A())
        t2.DisposeHelper_MethodOnDifferentType(new A())
        DisposeHelper_MultiLevelDown(new A())
    End Sub

    Sub DisposeHelper(a As A)
        a.Dispose()
    End Sub

    Sub DisposeHelper_MultiLevelDown(a As A)
        DisposeHelper(a)
    End Sub
End Class

Class Test2
    Sub DisposeHelper_MethodOnDifferentType(a As A)
        a.Dispose()
    End Sub
End Class
");
        }

        [Fact]
        public void Interprocedural_DisposeOwnershipTransfer_MethodInvocation_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1()
    {
        DisposeOwnershipTransfer(new A());
        var t2 = new Test2();
        t2.DisposeOwnershipTransfer_MethodOnDifferentType(new A());
        DisposeOwnershipTransfer_MultiLevelDown(new A());
    }

    void DisposeOwnershipTransfer(A a)
    {
        this.a = a;
    }

    void DisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        DisposeOwnershipTransfer(a);
    }
}

class Test2
{
    public A a;
    public void DisposeOwnershipTransfer_MethodOnDifferentType(A a)
    {
        this.a = a;
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public a As A
    Sub M1()
        DisposeOwnershipTransfer(new A())
        Dim t2 = New Test2()
        t2.DisposeOwnershipTransfer_MethodOnDifferentType(new A())
        DisposeOwnershipTransfer_MultiLevelDown(new A())
    End Sub

    Sub DisposeOwnershipTransfer(a As A)
        Me.a = a
    End Sub

    Sub DisposeOwnershipTransfer_MultiLevelDown(a As A)
        DisposeOwnershipTransfer(a)
    End Sub
End Class

Class Test2
    Public a As A
    Sub DisposeOwnershipTransfer_MethodOnDifferentType(a As A)
        Me.a = a
    End Sub
End Class
");
        }

        [Fact]
        public void Interprocedural_NoDisposeOwnershipTransfer_MethodInvocation_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1(Test2 t2)
    {
        NoDisposeOwnershipTransfer(new A(1));
        t2.NoDisposeOwnershipTransfer_MethodOnDifferentType(new A(2));
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(3));
    }

    void NoDisposeOwnershipTransfer(A a)
    {
        var str = a.ToString();
        var b = a;
    }

    void NoDisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        NoDisposeOwnershipTransfer(a);
    }
}

class Test2
{
    public A a;
    public void NoDisposeOwnershipTransfer_MethodOnDifferentType(A a)
    {
        var str = a.ToString();
        var b = a;
    }
}
",
            // Test0.cs(17,36): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(17, 36, "new A(1)"),
            // Test0.cs(18,61): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(18, 61, "new A(2)"),
            // Test0.cs(19,51): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(19, 51, "new A(3)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public a As A
    Sub M1(t2 As Test2)
        NoDisposeOwnershipTransfer(new A(1))
        t2.NoDisposeOwnershipTransfer_MethodOnDifferentType(new A(2))
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(3))
    End Sub

    Sub NoDisposeOwnershipTransfer(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub

    Sub NoDisposeOwnershipTransfer_MultiLevelDown(a As A)
        NoDisposeOwnershipTransfer(a)
    End Sub
End Class

Class Test2
    Public a As A
    Public Sub NoDisposeOwnershipTransfer_MethodOnDifferentType(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub
End Class
",
            // Test0.vb(16,36): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetBasicResultAt(16, 36, "new A(1)"),
            // Test0.vb(17,61): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetBasicResultAt(17, 61, "new A(2)"),
            // Test0.vb(18,51): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetBasicResultAt(18, 51, "new A(3)"));
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public void Interprocedural_DisposedInHelper_ConstructorInvocation_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        new DisposeHelperType(new A());
        DisposeHelper_MultiLevelDown(new A());
    }

    void DisposeHelper(A a)
    {
        new DisposeHelperType(a);
    }

    void DisposeHelper_MultiLevelDown(A a)
    {
        DisposeHelper(a);
    }
}

class DisposeHelperType
{
    public DisposeHelperType(A a)
    {
        a.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim unused = new DisposeHelperType(new A())
        DisposeHelper_MultiLevelDown(new A())
    End Sub

    Sub DisposeHelper(a As A)
        Dim unused = new DisposeHelperType(a)
    End Sub

    Sub DisposeHelper_MultiLevelDown(a As A)
        DisposeHelper(a)
    End Sub
End Class

Class DisposeHelperType
    Public Sub New(a As A)
        a.Dispose()
    End Sub
End Class
");
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public void Interprocedural_DisposeOwnershipTransfer_ConstructorInvocation_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        new DisposableOwnerType(new A());
        DisposeOwnershipTransfer_MultiLevelDown(new A());
    }

    void DisposeOwnershipTransfer(A a)
    {
        new DisposableOwnerType(a);
    }

    void DisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        DisposeOwnershipTransfer(a);
    }
}

class DisposableOwnerType
{
    public A a;
    public DisposableOwnerType(A a)
    {
        this.a = a;
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim unused = new DisposableOwnerType(new A())
        DisposeOwnershipTransfer_MultiLevelDown(new A())
    End Sub

    Sub DisposeOwnershipTransfer(a As A)
        Dim unused = new DisposableOwnerType(a)
    End Sub

    Sub DisposeOwnershipTransfer_MultiLevelDown(a As A)
        DisposeOwnershipTransfer(a)
    End Sub
End Class

Class DisposableOwnerType
    Public a As A
    Public Sub New(a As A)
        Me.a = a
    End Sub
End Class
");
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public void Interprocedural_NoDisposeOwnershipTransfer_ConstructorInvocation_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        new NotDisposableOwnerType(new A(1));
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(2));
    }

    void NoDisposeOwnershipTransfer(A a)
    {
        new NotDisposableOwnerType(a);
    }

    void NoDisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        NoDisposeOwnershipTransfer(a);
    }
}

class NotDisposableOwnerType
{
    public A a;
    public NotDisposableOwnerType(A a)
    {
        var str = a.ToString();
        var b = a;
    }
}
",
            // Test0.cs(16,36): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(16, 36, "new A(1)"),
            // Test0.cs(17,51): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(17, 51, "new A(2)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim unused = new NotDisposableOwnerType(new A(1))
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(2))
    End Sub

    Sub NoDisposeOwnershipTransfer(a As A)
        Dim unused = new NotDisposableOwnerType(a)
    End Sub

    Sub NoDisposeOwnershipTransfer_MultiLevelDown(a As A)
        NoDisposeOwnershipTransfer(a)
    End Sub
End Class

Class NotDisposableOwnerType
    Public a As A
    Public Sub New(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub
End Class
",
            // Test0.vb(15,49): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetBasicResultAt(15, 49, "new A(1)"),
            // Test0.vb(16,51): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetBasicResultAt(16, 51, "new A(2)"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Configured_DisposeOwnershipTransfer_AtConstructorInvocation(bool disposeOwnershipTransferAtConstructor)
        {
            var editorConfigText = disposeOwnershipTransferAtConstructor ?
                        $@"dotnet_code_quality.interprocedural_analysis_kind = None
                           dotnet_code_quality.dispose_ownership_transfer_at_constructor = true" :
                        string.Empty;
            var editorConfigFile = GetEditorConfigAdditionalFile(editorConfigText);

            var source = @"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    DisposableOwnerType M1()
    {
        return new DisposableOwnerType(new A());
    }
}

class DisposableOwnerType
{
    public DisposableOwnerType(A a)
    {
    }
}";

            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (!disposeOwnershipTransferAtConstructor)
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(15,40): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
                    GetCSharpResultAt(15, 40, "new A()")
                };
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Private Function M1() As DisposableOwnerType
        Return New DisposableOwnerType(New A())
    End Function
End Class

Class DisposableOwnerType
    Public Sub New(ByVal a As A)
    End Sub
End Class
";
            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (!disposeOwnershipTransferAtConstructor)
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(13,40): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
                    GetBasicResultAt(13, 40, "New A()")
                };
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Theory, WorkItem(1404, "https://github.com/dotnet/roslyn-analyzers/issues/1404")]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void DocsMicrosoft_Sample(DisposeAnalysisKind disposeAnalysisKind)
        {
            // See https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2000-dispose-objects-before-losing-scope

            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;

class Test
{
    public SerialPort OpenPort1(string portName)
    {
        SerialPort port = new SerialPort(portName);
        port.Open();  //CA2000 fires because this might throw
        SomeMethod(); //Other method operations can fail
        return port;
    }

    public SerialPort OpenPort2(string portName2)
    {
        SerialPort tempPort = null;
        SerialPort port = null;
        try
        {
            tempPort = new SerialPort(portName2);
            tempPort.Open();
            SomeMethod();
            //Add any other methods above this line
            port = tempPort;
            tempPort = null;

        }
        catch (Exception ex)
        {
        }
        finally
        {
            if (tempPort != null)
            {
                tempPort.Close();
            }
        }
        return port;
    }

    void SomeMethod()
    {
        Console.WriteLine(0);
    }
}

public class SerialPort : IDisposable
{
    public SerialPort(string portName)
    {
    }

    public void Dispose()
    {
    }

    public void Open()
    {
        Console.WriteLine(0);
    }

    public void Close()
    {
        Dispose();
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(8,27): warning CA2000: Object created by 'new SerialPort(portName)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(8, 27, "new SerialPort(portName)")
                };
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System

Class Test
    Public Function OpenPort1(portName As String) As SerialPort
        Dim port As SerialPort = New SerialPort(portName)
        port.Open()
        SomeMethod()
        Return port
    End Function

    Public Function OpenPort2(portName2 As String) As SerialPort
        Dim tempPort As SerialPort = Nothing
        Dim port As SerialPort = Nothing

        Try
            tempPort = New SerialPort(portName2)
            tempPort.Open()
            SomeMethod()
            port = tempPort
            tempPort = Nothing
        Catch ex As Exception
        Finally

            If tempPort IsNot Nothing Then
                tempPort.Close()
            End If
        End Try

        Return port
    End Function

    Private Sub SomeMethod()
        Console.WriteLine(0)
    End Sub
End Class

Public Class SerialPort
    Implements IDisposable

    Public Sub New(portName As String)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Open()
        Console.WriteLine(0)
    End Sub

    Public Sub Close()
        Dispose()
    End Sub
End Class
";
            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(6,34): warning CA2000: Object created by 'New SerialPort(portName)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(6, 34, "New SerialPort(portName)")
                };
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Fact, WorkItem(1404, "https://github.com/dotnet/roslyn-analyzers/issues/1404#issuecomment-446715696")]
        public void DisposableCreationInLoop()
        {
            VerifyBasic(@"
Imports System

Class Test
    Public Sub M()
        Dim disposeMe As IDisposable = Nothing
        Try
            For Each c In ""Foo""
                If disposeMe Is Nothing Then
                    disposeMe = New A()
                End If
            Next
        Finally
            If disposeMe IsNot Nothing Then
                disposeMe.Dispose()
            End If
        End Try
    End Sub
End Class

Public Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
");
        }

        [Theory, WorkItem(1404, "https://github.com/dotnet/roslyn-analyzers/issues/1404#issuecomment-446715696")]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void ExceptionInFinally(DisposeAnalysisKind disposeAnalysisKind)
        {
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsAndMayBeNotDisposedViolationsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(7,26): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetBasicMayBeNotDisposedOnExceptionPathsResultAt(7, 26, "New A(1)")
                };
            }

            VerifyBasic(@"
Imports System

Class Test
    Public Str As String
    Public Sub M()
        Dim disposeMe1 = New A(1)
        Dim disposeMe2 As A = Nothing
        Try
            disposeMe2 = New A(2)
            Integer.Parse(Str) ' Can throw
        Finally
            If disposeMe2 IsNot Nothing Then
                disposeMe2.Dispose()
            End If

            Integer.Parse(Str) ' Can throw

            disposeMe1.Dispose()
        End Try
    End Sub
End Class

Public Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
", GetEditorConfigFile(disposeAnalysisKind), expectedDiagnostics);
        }

        [Fact]
        public void LocalWithDisposableAssignment_DisposeBoolCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }

    public void Dispose(bool b)
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();
        a.Dispose(true);

        A b = new A();
        a = b;
        a.Dispose(true);
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub Dispose(b As Boolean)
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = New A()
        a.Dispose(true)

        Dim b As New A()
        a = b
        a.Dispose(true)
    End Sub
End Class");
        }

        [Fact]
        public void LocalWithDisposableAssignment_CloseCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }

    public void Close()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();
        a.Close();

        A b = new A();
        a = b;
        a.Close();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub Close()
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = New A()
        a.Close()

        Dim b As New A()
        a = b
        a.Close()
    End Sub
End Class");
        }

        [Fact, WorkItem(1796, "https://github.com/dotnet/roslyn-analyzers/issues/1796")]
        public void LocalWithDisposableAssignment_DisposeAsyncCall_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class A : IDisposable
{
    public void Dispose() => DisposeAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

class Test
{
    async Task M1()
    {
        A a;
        a = new A();
        await a.DisposeAsync();

        A b = new A();
        a = b;
        await a.DisposeAsync();
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeAsync()
    End Sub

    Public Function DisposeAsync() As Task
        Return Task.CompletedTask
    End Function
End Class

Class Test
    Async Function M1() As Task
        Dim a As A
        a = New A()
        Await a.DisposeAsync()

        Dim b As New A()
        a = b
        Await a.DisposeAsync()
    End Function
End Class");
        }

        [Fact]
        public void ArrayElementWithDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(A[] a)
    {
        a[0] = new A();     // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A())
        a(0) = New A()     ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class");
        }

        [Fact]
        public void ArrayElementWithDisposableAssignment_ConstantIndex_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(A[] a)
    {
        a[0] = new A();
        a[0].Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A())
        a(0) = New A()
        a(0).Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void ArrayElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(A[] a, int i)
    {
        a[i] = new A();
        a[i].Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A(), i As Integer)
        a(i) = New A()
        a(i).Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void ArrayElementWithDisposableAssignment_NonConstantIndex_02_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(A[] a, int i, int j)
    {
        a[i] = new A();
        i = j;              // Value of i is now unknown
        a[i].Dispose();     // We don't know the points to value of a[i], so don't flag 'new A()'
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A(), i As Integer, j As Integer)
        a(i) = New A()
        i = j               ' Value of i is now unknown
        a(i).Dispose()      ' We don't know the points to value of a(i), so don't flag 'New A()'
    End Sub
End Class");
        }

        [Fact]
        public void ArrayInitializer_ElementWithDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A[] a = new A[] { new A() };   // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A() = New A() {New A()}    ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class");
        }

        [Fact]
        public void ArrayInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A[] a = new A[] { new A() };
        a[0].Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A() = New A() {New A()}
        a(0).Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void ArrayInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(int i)
    {
        A[] a = new A[] { new A() };
        a[i].Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1(i As Integer)
        Dim a As A() = New A() {New A()}
        a(i).Dispose()
    End Sub
End Class");
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void CollectionInitializer_ElementWithDisposableAssignment_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        List<A> a = new List<A>() { new A() };   // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(17,37): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(17, 37, "new A()")
                };
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim a As List(Of A) = New List(Of A) From {New A()}    ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class";

            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(14,52): warning CA2000: Object created by 'New A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(14, 52, "New A()")
                };
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void CollectionInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        List<A> a = new List<A>() { new A() };
        a[0].Dispose();
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(17,37): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(17, 37, "new A()")
                };
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim a As List(Of A) = New List(Of A) From {New A()}
        a(0).Dispose()
    End Sub
End Class";

            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(14,52): warning CA2000: Object created by 'New A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(14, 52, "New A()")
                };
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void CollectionInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(int i)
    {
        List<A> a = new List<A>() { new A() };
        a[i].Dispose();
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(17,37): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(17, 37, "new A()")
                };
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1(i As Integer)
        Dim a As List(Of A) = New List(Of A) From {New A()}
        a(i).Dispose()
    End Sub
End Class";

            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.vb(14,52): warning CA2000: Object created by 'New A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(14, 52, "New A()")
                };
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void CollectionAdd_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class NonGenericList : ICollection
{
    public void Add(A item)
    {
    }

    public int Count => throw new NotImplementedException();

    public object SyncRoot => throw new NotImplementedException();

    public bool IsSynchronized => throw new NotImplementedException();

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class Test
{
    void M1()
    {
        List<A> a = new List<A>();
        a.Add(new A(1));

        A b = new A(2);
        a.Add(b);

        NonGenericList l = new NonGenericList();
        l.Add(new A(3));

        b = new A(4);
        l.Add(b);
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                // NOTE: 'new A(3)' and 'new A(4)' are not flagged as they invoke Add method defined in source which is a no-op.

                var builder = new List<DiagnosticResult>();
                if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
                {
                    builder.Add(
                        // Test0.cs(42,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(42, 15, "new A(1)"));
                }

                builder.Add(
                    // Test0.cs(44,15): warning CA2000: Object created by 'new A(2)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(44, 15, "new A(2)"));

                expectedDiagnostics = builder.ToArray();
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System
Imports System.Collections
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class NonGenericList
    Implements ICollection

    Public Sub Add(item As A)
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
        Throw New NotImplementedException()
    End Sub

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test
    Private Sub M1()
        Dim a As New List(Of A)()
        a.Add(New A(1))

        Dim b As A = New A(2)
        a.Add(b)

        Dim l As New NonGenericList()
        l.Add(New A(3))

        b = New A(4)
        l.Add(b)
    End Sub
End Class";

            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                // NOTE: 'new A(3)' and 'new A(4)' are not flagged as they invoke Add method defined in source which is a no-op.

                var builder = new List<DiagnosticResult>();
                if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
                {
                    builder.Add(
                        // Test0.vb(51,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetBasicMayBeNotDisposedOnExceptionPathsResultAt(51, 15, "New A(1)"));
                }

                builder.Add(
                    // Test0.vb(53,22): warning CA2000: Object created by 'New A(2)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(53, 22, "New A(2)"));

                expectedDiagnostics = builder.ToArray();
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void CollectionAdd_IReadOnlyCollection_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class MyReadOnlyCollection : IReadOnlyCollection<A>
{
    public void Add(A item)
    {
    }
    
    public int Count => throw new NotImplementedException();

    public IEnumerator<A> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class Test
{
    void M1()
    {
        var myReadOnlyCollection = new MyReadOnlyCollection();
        myReadOnlyCollection.Add(new A(1));
        A a = new A(2);
        myReadOnlyCollection.Add(a);

        var builder = ImmutableArray.CreateBuilder<A>();
        builder.Add(new A(3));
        A a2 = new A(4);
        builder.Add(a2);

        var bag = new ConcurrentBag<A>();
        builder.Add(new A(5));
        A a3 = new A(6);
        builder.Add(a3);
    }
}
";
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                var builder = new List<DiagnosticResult>();
                if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
                {
                    builder.AddRange(new[]
                    {
                        // NOTE: 'new A(1)' and 'new A(2)' are not flagged as they invoke Add method defined in source which is a no-op.

                        // Test0.cs(45,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(3)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(45, 21, "new A(3)"),
                        // Test0.cs(46,16): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(4)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(46, 16, "new A(4)"),
                        // Test0.cs(50,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(5)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(50, 21, "new A(5)"),
                    });
                }

                builder.Add(
                    // Test0.cs(51,16): warning CA2000: Object created by 'new A(6)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(51, 16, "new A(6)"));

                expectedDiagnostics = builder.ToArray();
            }

            VerifyCSharp(source, editorConfigFile, expectedDiagnostics);

            source = @"
Imports System
Imports System.Collections
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class MyReadOnlyCollection
    Implements IReadOnlyCollection(Of A)

    Public Sub Add(ByVal item As A)
    End Sub

    Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of A).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator(Of A) Implements IEnumerable(Of A).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test
    Private Sub M1()
        Dim myReadOnlyCollection = New MyReadOnlyCollection()
        myReadOnlyCollection.Add(New A(1))
        Dim a As A = New A(2)
        myReadOnlyCollection.Add(a)

        Dim builder = ImmutableArray.CreateBuilder(Of A)()
        builder.Add(New A(3))
        Dim a2 As A = New A(4)
        builder.Add(a2)

        Dim bag = New ConcurrentBag(Of A)()
        builder.Add(New A(5))
        Dim a3 As A = New A(6)
        builder.Add(a3)
    End Sub
End Class";

            expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                var builder = new List<DiagnosticResult>();
                if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
                {
                    builder.AddRange(new[]
                    {
                        // NOTE: 'New A(1)' and 'New A(2)' are not flagged as they invoke Add method defined in source which is a no-op.

                        // Test0.vb(46,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(3)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetBasicMayBeNotDisposedOnExceptionPathsResultAt(46, 21, "New A(3)"),
                        // Test0.vb(47,23): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(4)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetBasicMayBeNotDisposedOnExceptionPathsResultAt(47, 23, "New A(4)"),
                        // Test0.vb(51,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(5)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                        GetBasicMayBeNotDisposedOnExceptionPathsResultAt(51, 21, "New A(5)")
                    });
                }

                builder.Add(
                    // Test0.vb(52,23): warning CA2000: Object created by 'New A(6)' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(52, 23, "New A(6)"));

                expectedDiagnostics = builder.ToArray();
            }

            VerifyBasic(source, editorConfigFile, expectedDiagnostics);
        }

        [Fact]
        public void MemberInitializerWithDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public int X;
    public void Dispose()
    {

    }
}

class Test
{
    public A a;
    void M1()
    {
        var a = new Test { a = { X = 0 } };
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public X As Integer
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public a As A
    Sub M1()
        Dim a = New Test With {.a = New A() With { .X = 1 }}
    End Sub
End Class");
        }

        [Fact]
        public void StructImplementingIDisposable_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

struct A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        var a = new A();
    }
}
");

            VerifyBasic(@"
Imports System

Structure A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Structure

Class Test
    Sub M1()
        Dim a As New A()
    End Sub
End Class");
        }

        [Fact]
        public void NonUserDefinedConversions_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : A
{
}

class Test
{
    void M1()
    {
        object obj = new A();   // Implicit conversion from A to object
        ((A)obj).Dispose();     // Explicit conversion from object to A

        A a = new B();          // Implicit conversion from B to A     
        a.Dispose();        
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Sub M1()
        Dim obj As Object = New A()             ' Implicit conversion from A to object
        DirectCast(obj, A).Dispose()            ' Explicit conversion from object to A
        
        Dim a As A = new B()                    ' Implicit conversion from B to A
        a.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void NonUserDefinedConversions_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : A
{
}

class Test
{
    void M1()
    {
        object obj = new A();   // Implicit conversion from A to object
        A a = (A)new B();       // Explicit conversion from B to A
    }
}
",
            // Test0.cs(20,22): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(20, 22, "new A()"),
            // Test0.cs(21,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'new B()' before all references to it are out of scope.
            GetCSharpResultAt(21, 18, "new B()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Sub M1()
        Dim obj As Object = New A()             ' Implicit conversion from A to object        
        Dim a As A = DirectCast(New B(), A)     ' Explicit conversion from B to A
    End Sub
End Class",
            // Test0.vb(17,29): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(17, 29, "New A()"),
            // Test0.vb(18,33): warning CA2000: Call System.IDisposable.Dispose on object created by 'New B()' before all references to it are out of scope.
            GetBasicResultAt(18, 33, "New B()"));
        }

        [Fact]
        public void UserDefinedConversions_NoDiagnostic()
        {
            VerifyCSharp(@"

using System;

class A : IDisposable
{
    public void Dispose()
    {

    }

    public static implicit operator A(B value)
    {
        value.Dispose();
        return null;
    }

    public static explicit operator B(A value)
    {
        value.Dispose();
        return null;
    }
}

class B : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    Test(string s)
    {
    }

    void M1()
    {
        A a = new B();      // Implicit user defined conversion
        B b = (B)new A();   // Explicit user defined conversion
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Shared Widening Operator CType(ByVal value As A) As B
        value.Dispose()
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(ByVal value As B) As A
        value.Dispose()
        Return Nothing
    End Operator
End Class

Class B
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Private Sub M1()
        Dim a As A = New B()            ' Implicit user defined conversion
        Dim b As B = CType(New A(), B)  ' Explicit user defined conversion
    End Sub
End Class");
        }

        [Fact]
        public void LocalWithDisposableAssignment_ByRef_DisposedInCallee_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = new A();
        M2(ref a);
    }

    void M2(ref A a)
    {
        a.Dispose();
        a = null;
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim a As New A()
        M2(a)
    End Sub

    Sub M2(ByRef a as A)
        a.Dispose()
        a = Nothing
    End Sub
End Class");
        }

        [Fact]
        public void LocalWithDisposableAssignment_ByRefEscape_AbstractVirtualMethod_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {

    }
}

public abstract class Test
{
    void M1()
    {
        A a = new A();
        M2(ref a);

        a = new A();
        M3(ref a);
    }

    public virtual void M2(ref A a)
    {
    }

    public abstract void M3(ref A a);
}
");

            VerifyBasic(@"
Imports System

Public Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public MustInherit Class Test
    Sub M1()
        Dim a As New A()
        M2(a)

        a = New A()
        M3(a)
    End Sub

    Public Overridable Sub M2(ByRef a as A)
    End Sub

    Public MustOverride Sub M3(ByRef a as A)
End Class");
        }

        [Fact]
        public void LocalWithDisposableAssignment_OutRefKind_NotDisposed_Diagnostic()
        {
            // Local/parameter passed as out is not considered escaped.
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = new A();
        M2(out a);
    }

    void M2(out A a)
    {
        a = new A();
    }
}
",
            // Test0.cs(16,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(16, 15, "new A()"),
            // Test0.cs(17,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a' before all references to it are out of scope.
            GetCSharpResultAt(17, 12, "out a"));
        }

        [Fact]
        public void LocalWithDefaultOfDisposableAssignment_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = default(A);
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A = Nothing
    End Sub
End Module");
        }

        [Fact]
        public void NullCoalesce_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(A a)
    {
        A b = a ?? new A();
        b.Dispose();

        A c = new A();
        A d = c ?? a;
        d.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1(a As A)
        Dim b As A = If(a, New A())
        b.Dispose()

        Dim c As New A()
        Dim d As A = If(c, a)
        d.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void NullCoalesce_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        A b = a ?? new A();
        a.Dispose();

        a = new A();
        A c = a ?? new A();
        c.Dispose();
    }
}
",
            // Test0.cs(15,20): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(15, 20, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        Dim b As A = If(a, New A())
        a.Dispose()

        a = New A()
        Dim c As A = If(a, New A())
        c.Dispose()
    End Sub
End Class",
            // Test0.vb(12,28): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(12, 28, "New A()"));
        }

        [Fact]
        public void WhileLoop_DisposeOnBackEdge_NoDiagnostic()
        {
            // Need precise CFG to avoid false reports.
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(bool flag)
    {
        A a = new A();
        while (true)
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            }
            a = new A();
        }
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1(flag As Boolean)
        Dim a As New A()
        While True
            a.Dispose()
            If flag Then
                Exit While    ' All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            End If
            a = New A()
        End While
    End Sub
End Module");
        }

        [Fact, WorkItem(1648, "https://github.com/dotnet/roslyn-analyzers/issues/1648")]
        public void WhileLoop_MissingDisposeOnExit_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = new A(1);  // Allocated outside the loop and disposed inside a loop is not a recommended pattern and is flagged.
        while (true)
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        }
    }
}
",
            // Test0.cs(17,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(17, 15, "new A(1)"),
            // Test0.cs(21,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(21, 17, "new A(2)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A(1)   ' Allocated outside the loop and disposed inside a loop is not a recommended pattern and is flagged.
        While True
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        End While
    End Sub
End Module",
            // Test0.vb(16,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(16, 18, "New A(1)"),
            // Test0.vb(19,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A(2)' before all references to it are out of scope.
            GetBasicResultAt(19, 17, "New A(2)"));
        }

        [Fact]
        public void WhileLoop_MissingDisposeOnEntry_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

class Test
{
    public bool Flag;
    void M1()
    {
        A a;      
        while ((a = new A(1)) != null)   // This instance will never be disposed, but is not flagged as there is no feasible loop exit.
        {
            a = new A(2);
            a.Dispose();
        }
    }

    void M2(bool flag)
    {
        A a;      
        while ((a = new A(3)) != null)   // This instance will never be disposed on loop exit.
        {
            if (Flag)
            {
                break;
            }
            a = new A(4);
            a.Dispose();
        }
    }
}
",
            // Test0.cs(29,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(29, 21, "new A(3)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A()    ' This instance will never be disposed.
        While True
            a = New A()
            a.Dispose()
        End While
    End Sub
End Module",
            // Test0.vb(13,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 18, "New A()"));
        }

        [Fact]
        public void DoWhileLoop_DisposeOnBackEdge_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(bool flag)
    {
        A a = new A();
        do
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            }
            a = new A();
        } while (true);
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1(flag As Boolean)
        Dim a As New A()
        Do While True
            a.Dispose()
            If flag Then
                Exit Do    ' All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            End If
            a = New A()
        Loop
    End Sub
End Module");
        }

        [Fact]
        public void DoWhileLoop_MissingDisposeOnExit_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a = new A(1);
        do
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        } while (true);
    }
}
",
            // Test0.cs(20,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(20, 17, "new A(2)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A(1)
        Do
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        Loop While True
    End Sub
End Module",
            // Test0.vb(18,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A(2)' before all references to it are out of scope.
            GetBasicResultAt(18, 17, "New A(2)"));
        }

        [Fact]
        public void DoWhileLoop_MissingDisposeOnEntry_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;      
        do
        {
            a = new A(1);
            a.Dispose();
        } while ((a = new A(2)) != null);   // This instance will never be disposed, but it is not flagged as there is no feasible loop exit.
    }

    void M2()
    {
        A a = null;      
        do
        {
            if (a != null)
            {
                break;
            }
            a = new A(3);
            a.Dispose();
        } while ((a = new A(4)) != null);   // This instance will never be disposed.
    }
}
",
            // Test0.cs(36,23): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(4)' before all references to it are out of scope.
            GetCSharpResultAt(36, 23, "new A(4)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A()    ' This instance will never be disposed.
        Do While True
            a = New A()
            a.Dispose()
        Loop
    End Sub
End Module",
            // Test0.vb(13,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 18, "New A()"));
        }

        [Fact]
        public void ForLoop_DisposeOnBackEdge_NoDiagnostic()
        {
            // Need precise CFG to avoid false reports.
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1(bool flag)
    {
        A a = new A(1);      // Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        for (int i = 0; i < 10; i++)
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path.
            }

            a = new A(2); // This can leak on loop exit, and is flagged as a maybe disposed violation.
        }
    }
}
",
            // Test0.cs(16,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(16, 15, "new A(1)"),
            // Test0.cs(25,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(25, 17, "new A(2)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    Sub M1(flag As Boolean)
        Dim a As New A(1)   ' Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        For i As Integer = 0 To 10
            a.Dispose()
            If flag Then
                Exit For    ' All 'A' instances have been disposed on this path.
            End If
            a = New A(2)    ' This can leak on loop exit, and is flagged as a maybe disposed violation.
        Next
    End Sub
End Module",
            // Test0.vb(15,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(15, 18, "New A(1)"),
            // Test0.vb(21,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(21, 17, "New A(2)"));
        }

        [Fact]
        public void ForLoop_MissingDisposeOnExit_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = new A(1);  // Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        for (int i = 0; i < 10; i++)
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        }
    }
}
",
            // Test0.cs(17,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(17, 15, "new A(1)"),
            // Test0.cs(21,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(21, 17, "new A(2)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A(1)    ' Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        For i As Integer = 0 To 10
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        Next
    End Sub
End Module",
            // Test0.vb(16,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(16, 18, "New A(1)"),
            // Test0.vb(19,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A(2)' before all references to it are out of scope.
            GetBasicResultAt(19, 17, "New A(2)"));
        }

        [Fact]
        public void ForLoop_MissingDisposeOnEntry_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;
        int i;
        for (i = 0, a = new A(); i < 10; i++)   // This 'A' instance will never be disposed.
        {
            a = new A();
            a.Dispose();
        }
    }
}
",
            // Test0.cs(18,25): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(18, 25, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A()    ' This instance will never be disposed.
        For i As Integer = 0 To 10
            a = New A()
            a.Dispose()
        Next
    End Sub
End Module",
            // Test0.vb(13,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 18, "New A()"));
        }

        [Fact]
        public void IfStatement_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : A
{
}

class Test
{
    void M1(A a, string param)
    {
        A a1 = new A();
        B a2 = new B();
        A b;
        if (param != null)
        {
            a = a1;
            b = new B();
        }
        else 
        {
            a = a2;
            b = new A();
        }
        
        a.Dispose();         // a points to either a1 or a2.
        b.Dispose();         // b points to either instance created in if or else.
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Private Sub M1(a As A, param As String)
        Dim a1 As New A()
        Dim a2 As B = new B()
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = new B()
        Else
            a = a2
            b = new A()
        End If
        
        a.Dispose()          ' a points to either a1 or a2.
        b.Dispose()          ' b points to either instance created in if or else.
    End Sub
End Class");
        }

        [Fact]
        public void IfStatement_02_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : A
{
}

class Test
{
    void M1(A a, string param, string param2)
    {
        A a1 = new A();
        B a2 = new B();
        A b;
        if (param != null)
        {
            a = a1;
            b = new B();

            if (param == """")
            {
                a = new B();
            }
            else
            {
                if (param2 != null)
                {
                    b = new A();
                }
                else
                {
                    b = new B();
                }
            }
        }
        else 
        {
            a = a2;
            b = new A();
        }
        
        a.Dispose();         // a points to either a1 or a2 or instance created in 'if(param == """")'.
        b.Dispose();         // b points to either instance created in outer if or outer else or innermost if or innermost else.
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Private Sub M1(a As A, param As String, param2 As String)
        Dim a1 As New A()
        Dim a2 As B = new B()
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = new B()
            If param = """" Then
                a = new B()
            Else
                If param2 IsNot Nothing Then
                    b = new A()
                Else
                    b = new B()
                End If
            End If
        Else
            a = a2
            b = new A()
        End If

        a.Dispose()          ' a points to either a1 or a2 or instance created in 'if(param == """")'.
        b.Dispose()          ' b points to either instance created in outer if or outer else or innermost if or innermost else.
    End Sub
End Class");
        }

        [Fact]
        public void IfStatement_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class C : B
{
}

class D : C
{
}

class E : D
{
}

class Test
{
    void M1(A a, string param, string param2)
    {
        A a1 = new A();     // Maybe disposed.
        B a2 = new B();     // Never disposed.
        A b;
        if (param != null)
        {
            a = a1;
            b = new C();     // Never disposed.
        }
        else
        {
            a = a2;
            b = new D();     // Never disposed.
        }
        
        // a points to either a1 or a2.
        // b points to either instance created in if or else.

        if (param != null)
        {
            A c = new A();
            a = c;
            b = a1;
        }
        else 
        {
            C d = new E();
            b = d;
            a = b;
        }

        a.Dispose();         // a points to either c or d.
        b.Dispose();         // b points to either a1 or d.
    }
}
",
            // Test0.cs(32,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new B()' before all references to it are out of scope.
            GetCSharpResultAt(32, 16, "new B()"),
            // Test0.cs(37,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new C()' before all references to it are out of scope.
            GetCSharpResultAt(37, 17, "new C()"),
            // Test0.cs(42,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new D()' before all references to it are out of scope.
            GetCSharpResultAt(42, 17, "new D()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class C
    Inherits B
End Class

Class D
    Inherits C
End Class

Class E
    Inherits D
End Class

Class Test
    Private Sub M1(ByVal a As A, ByVal param As String, ByVal param2 As String)
        Dim a1 As A = New A()   ' Maybe disposed.
        Dim a2 As B = New B()   ' Never disposed.
        Dim b As A

        If param IsNot Nothing Then
            a = a1
            b = New C()     ' Never disposed.
        Else
            a = a2
            b = New D()     ' Never disposed.
        End If

        ' a points to either a1 or a2.
        ' b points to either instance created in if or else.

        If param IsNot Nothing Then
            Dim c As A = New A()
            a = c
            b = a1
        Else
            Dim d As C = New E()
            b = d
            a = b
        End If

        a.Dispose()         ' a points to either c or d.
        b.Dispose()         ' b points to either a1 or d.
    End Sub
End Class",
            // Test0.vb(29,23): warning CA2000: Call System.IDisposable.Dispose on object created by 'New B()' before all references to it are out of scope.
            GetBasicResultAt(29, 23, "New B()"),
            // Test0.vb(34,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New C()' before all references to it are out of scope.
            GetBasicResultAt(34, 17, "New C()"),
            // Test0.vb(37,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New D()' before all references to it are out of scope.
            GetBasicResultAt(37, 17, "New D()"));
        }

        [Fact]
        public void IfStatement_02_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class C : B
{
}

class D : C
{
}

class E : D
{
}

class Test
{
    void M1(A a, string param, string param2)
    {
        A a1 = new B();     // Never disposed
        B a2 = new C();     // Never disposed
        A b;
        if (param != null)
        {
            a = a1;
            b = new A();     // Maybe disposed

            if (param == """")
            {
                a = new D();     // Never disposed
            }
            else
            {
                if (param2 != null)
                {
                    b = new A();    // Maybe disposed
                }
                else
                {
                    b = new A();    // Maybe disposed
                    if (param == """")
                    {
                        b = new A();    // Maybe disposed
                    }
                }
                
                if (param2 == """")
                {
                    b.Dispose();    // b points to one of the three instances of A created above.
                    b = new A();    // Always disposed
                }
            }
        }
        else 
        {
            a = a2;
            b = new A();        // Maybe disposed
            if (param2 != null)
            {
                a = new A();    // Always disposed
            }
            else
            {
                a = new A();    // Always disposed
                b = new A();    // Always disposed
            }

            a.Dispose();
        }

        b.Dispose();         
    }
}
",
            // Test0.cs(31,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new B()' before all references to it are out of scope.
            GetCSharpResultAt(31, 16, "new B()"),
            // Test0.cs(32,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new C()' before all references to it are out of scope.
            GetCSharpResultAt(32, 16, "new C()"),
            // Test0.cs(41,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new D()' before all references to it are out of scope.
            GetCSharpResultAt(41, 21, "new D()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class C
    Inherits B
End Class

Class D
    Inherits C
End Class

Class E
    Inherits D
End Class

Class Test

    Private Sub M1(ByVal a As A, ByVal param As String, ByVal param2 As String)
        Dim a1 As A = New B()       ' Never disposed
        Dim a2 As B = New C()       ' Never disposed
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = New A()       ' Always disposed
            If param = """" Then
                a = New D()       ' Never disposed
            Else
                If param2 IsNot Nothing Then
                    b = New A()       ' Maybe disposed
                Else
                    b = New A()       ' Maybe disposed
                    If param = """" Then
                        b = New A()   ' Maybe disposed
                    End If
                End If

                If param2 = """" Then
                    b.Dispose()     ' b points to one of the three instances of A created above.
                    b = New A()     ' Always disposed
                End If
            End If
        Else
            a = a2
            b = New A()       ' Maybe disposed
            If param2 IsNot Nothing Then
                a = New A()       ' Always disposed
            Else
                a = New A()       ' Always disposed
                b = New A()       ' Always disposed
            End If

            a.Dispose()
        End If

        b.Dispose()
    End Sub
End Class",
                // Test0.vb(29,23): warning CA2000: Call System.IDisposable.Dispose on object created by 'New B()' before all references to it are out of scope.
                GetBasicResultAt(29, 23, "New B()"),
                // Test0.vb(30,23): warning CA2000: Call System.IDisposable.Dispose on object created by 'New C()' before all references to it are out of scope.
                GetBasicResultAt(30, 23, "New C()"),
                // Test0.vb(36,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'New E()' before all references to it are out of scope.
                GetBasicResultAt(36, 21, "New D()"));
        }

        [Fact]
        public void UsingStatement_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        using (var a = new A())
        {
        }

        A b;
        using (b = new A())
        {
        }

        using (A c = new A(), d = new A())
        {
        }

        A e = new A();
        using (e)
        {
        }

        using (A f = null)
        {
        }
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Private Sub M1()
        Using a As New A()
        End Using

        Dim b As A = New A()
        Using b
        End Using

        Using c As New A(), d = New A()
        End Using

        Using a As A = Nothing
        End Using
    End Sub
End Class");
        }

        [Fact, WorkItem(2201, "https://github.com/dotnet/roslyn-analyzers/issues/2201")]
        public void UsingStatementInTryCatch_NoDiagnostic()
        {
            VerifyCSharp(@"
using System.IO;

class Test
{
    void M1()
    {
        try
        {
            using (var ms = new MemoryStream())
            {
            }
        }
        catch
        {
        }
    }
}");

            VerifyBasic(@"
Imports System.IO

Class Test
    Private Sub M1()
        Try
            Using ms = New MemoryStream()
            End Using
        Catch
        End Try
    End Sub
End Class");
        }

        [Fact, WorkItem(2201, "https://github.com/dotnet/roslyn-analyzers/issues/2201")]
        public void NestedTryFinallyInTryCatch_NoDiagnostic()
        {
            VerifyCSharp(@"
using System.IO;

class Test
{
    void M1()
    {
        try
        {
            var ms = new MemoryStream();
            try
            {
            }
            finally
            {
                ms?.Dispose();
            }
        }
        catch
        {
        }
    }
}");

            VerifyBasic(@"
Imports System.IO

Class Test
    Private Sub M1()
        Try
            Dim ms = New MemoryStream()
            Try
            Finally
                ms?.Dispose()
            End Try
        Catch
        End Try
    End Sub
End Class");
        }

        [Fact]
        public void ReturnStatement_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    A M1()
    {
        return new A();
    }

    A M2(A a)
    {
        a = new A();
        return a;
    }

    A M3(A a)
    {
        a = new A();
        A b = a;
        return b;
    }

    A M4(A a) => new A();

    IEnumerable<A> M5()
    {
        yield return new A();
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Private Function M1() As A
        Return New A()
    End Function

    Private Function M2(a As A) As A
        a = New A()
        Return a
    End Function

    Private Function M3(a As A) As A
        a = New A()
        Dim b = a
        Return b
    End Function

    Public Iterator Function M4() As IEnumerable(Of A)
        Yield New A
    End Function
End Class");
        }

        [Fact]
        public void ReturnStatement_02_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : I, IDisposable
{
    public void Dispose()
    {

    }
}

interface I
{
}

class Test
{
    I M1()
    {
        return new A();
    }

    I M2()
    {
        return new A() as I;
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic

Class A
    Implements I, IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Interface I
End Interface

Class Test

    Private Function M1() As I
        Return New A()
    End Function

    Private Function M2() As I
        Return TryCast(New A(), I)
    End Function
End Class");
        }

        [Fact]
        public void LocalFunctionInvocation_EmptyBody_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();

        void MyLocalFunction()
        {
        };

        MyLocalFunction();    // This should not change state of 'a'.
    }
}
",
            // Test0.cs(17,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(17, 13, "new A()"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunctionInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a = new A();

        void MyLocalFunction()
        {
            a.Dispose();
        };

        MyLocalFunction();    // This should change state of 'a' to be Disposed.
    }
}
");

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunctionInvocation_CapturedValueAssignedNewDisposable_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;

        void MyLocalFunction()
        {
            a = new A();
        };

        MyLocalFunction();    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }
}
",
            // Test0.cs(20,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(20, 17, "new A()"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunctionInvocation_ChangesCapturedValueContextSensitive_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;

        void MyLocalFunction(A b)
        {
            a = b;
        };

        MyLocalFunction(new A());    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }
}
",
            // Test0.cs(23,25): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(23, 25, "new A()"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationNotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        void MyLocalFunction()
        {
            A a = new A();  // This should fire a diagnostic.
        };

        MyLocalFunction();
    }
}
",
            // Test0.cs(18,19): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(18, 19, "new A()"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreation_InvokedMultipleTimes_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        void MyLocalFunction()
        {
            A a = new A();  // This should fire a single diagnostic.
        };

        MyLocalFunction();
        MyLocalFunction();
    }
}
",
            // Test0.cs(18,19): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(18, 19, "new A()"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationReturned_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A MyLocalFunction()
        {
            return new A();
        };

        var a = MyLocalFunction(/*1*/);  // This should fire a diagnostic.
        var b = MyLocalFunction(/*2*/);  // This should fire a diagnostic.
    }
}
",
            // Test0.cs(21,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'MyLocalFunction(/*1*/)' before all references to it are out of scope.
            GetCSharpResultAt(21, 17, "MyLocalFunction(/*1*/)"),
            // Test0.cs(22,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'MyLocalFunction(/*2*/)' before all references to it are out of scope.
            GetCSharpResultAt(22, 17, "MyLocalFunction(/*2*/)"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationReturned_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A MyLocalFunction()
        {
            return new A();
        };

        var a = MyLocalFunction();
        a.Dispose();
    }
}
");

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationAssignedToRefOutParameter_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref a1, out a2);  // This should fire two diagnostics.
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }
}
",
            // Test0.cs(17,25): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref a1' before all references to it are out of scope.
            GetCSharpResultAt(17, 25, "ref a1"),
            // Test0.cs(17,33): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a2' before all references to it are out of scope.
            GetCSharpResultAt(17, 33, "out a2"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationAssignedToRefOutParameter_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref a1, out a2);
        a1.Dispose();
        a2.Dispose();
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }
}
");

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreationAssignedToRefOutParameter_MultipleCalls_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
        MyLocalFunction(ref /*2*/a1, out /*2*/a2);    // No diagnostics.
        a1.Dispose();
        a2.Dispose();
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }
}
",
            // Test0.cs(16,25): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(16, 25, "ref /*1*/a1"),
            // Test0.cs(16,38): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(16, 38, "out /*1*/a2"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreation_MultipleLevelsBelow_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a1 = null, a2;
        MyLocalFunction1(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
        return;

        void MyLocalFunction1(ref A param1, out A param2)
        {
            MyLocalFunction2(ref /*2*/param1, out /*2*/param2);
        };

        void MyLocalFunction2(ref A param3, out A param4)
        {
            param3 = new A();
            param4 = new A();
        };
    }
}
",
            // Test0.cs(17,26): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(17, 26, "ref /*1*/a1"),
            // Test0.cs(17,39): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(17, 39, "out /*1*/a2"));

            // VB has no local functions.
        }

        [Fact]
        public void LocalFunction_DisposableCreation_MultipleLevelsBelow_Nested_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a1 = null, a2;
        MyLocalFunction1(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
        return;

        void MyLocalFunction1(ref A param1, out A param2)
        {
            MyLocalFunction2(ref /*2*/param1, out /*2*/param2);

            void MyLocalFunction2(ref A param3, out A param4)
            {
                param3 = new A();
                param4 = new A();
            };
        };
    }
}
",
            // Test0.cs(17,26): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(17, 26, "ref /*1*/a1"),
            // Test0.cs(17,39): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(17, 39, "out /*1*/a2"));

            // VB has no local functions.
        }

        [Fact]
        public void LambdaInvocation_EmptyBody_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();

        System.Action myLambda = () =>
        {
        };

        myLambda();    // This should not change state of 'a'.
    }
}
",
            // Test0.cs(17,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(17, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A
        a = New A()

        Dim myLambda As System.Action = Sub()
                                        End Sub

        myLambda()      ' This should not change state of 'a'.
    End Sub
End Module",
            // Test0.vb(14,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(14, 13, "New A()"));
        }

        [Fact]
        public void LambdaInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{

    void M1()
    {
        A a = new A();

        System.Action myLambda = () =>
        {
            a.Dispose();
        };

        myLambda();    // This should change state of 'a' to be Disposed.
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As New A()

        Dim myLambda As System.Action = Sub()
                                            a.Dispose()
                                        End Sub

        myLambda()      '  This should change state of 'a' to be Disposed.
    End Sub
End Module");
        }

        [Fact]
        public void LambdaInvocation_CapturedValueAssignedNewDisposable_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{

    void M1()
    {
        A a;

        System.Action myLambda = () =>
        {
            a = new A();
        };

        myLambda();    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }
}
",
            // Test0.cs(21,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.            
            GetCSharpResultAt(21, 17, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A

        Dim myLambda As System.Action = Sub()
                                            a = New A()
                                        End Sub

        myLambda()      ' This should change state of 'a' to be NotDisposed and fire a diagnostic.
    End Sub
End Module",
            // Test0.vb(16,49): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(16, 49, "New A()"));
        }

        [Fact]
        public void LambdaInvocation_ChangesCapturedValueContextSensitive_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;

        System.Action<A> myLambda = b =>
        {
            a = b;
        };

        myLambda(new A());    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }
}
",
            // Test0.cs(23,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(23, 18, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A

        Dim myLambda As System.Action(Of A) = Sub(b As A)
                                                a = b
                                              End Sub

        myLambda(New A())      ' This should change state of 'a' to be NotDisposed and fire a diagnostic.
    End Sub
End Module",
            // Test0.vb(19,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(19, 18, "New A()"));
        }

        [Fact]
        public void Lambda_DisposableCreationNotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        System.Action myLambda = () =>
        {
            A a = new A();  // This should fire a diagnostic.
        };

        myLambda();
    }
}
",
            // Test0.cs(18,19): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(18, 19, "new A()"));
        }

        [Fact]
        public void Lambda_DisposableCreation_InvokedMultipleTimes_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        System.Action myLambda = () =>
        {
            A a = new A();  // This should fire a single diagnostic.
        };

        myLambda();
        myLambda();
    }
}
",
            // Test0.cs(18,19): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(18, 19, "new A()"));
        }

        [Fact]
        public void Lambda_DisposableCreationReturned_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        System.Func<A> myLambda = () =>
        {
            return new A();
        };

        var a = myLambda(/*1*/);  // This should fire a diagnostic.
        var b = myLambda(/*2*/);  // This should fire a diagnostic.
    }
}
",
            // Test0.cs(21,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'myLambda(/*1*/)' before all references to it are out of scope.
            GetCSharpResultAt(21, 17, "myLambda(/*1*/)"),
            // Test0.cs(22,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'myLambda(/*1*/)' before all references to it are out of scope.
            GetCSharpResultAt(22, 17, "myLambda(/*2*/)"));
        }

        [Fact]
        public void Lambda_DisposableCreationReturned_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        System.Func<A> myLambda = () =>
        {
            return new A();
        };

        var a = myLambda();
        a.Dispose();
    }
}
");
        }

        [Fact]
        public void Lambda_DisposableCreationAssignedToRefOutParameter_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref a1, out a2);  // This should fire two diagnostics.
        return;
    }
}
",
            // Test0.cs(24,20): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref a1' before all references to it are out of scope.
            GetCSharpResultAt(24, 20, "ref a1"),
            // Test0.cs(24,28): warning CA2000: Call System.IDisposable.Dispose on object created by 'out a2' before all references to it are out of scope.
            GetCSharpResultAt(24, 28, "out a2"));
        }

        [Fact]
        public void Lambda_DisposableCreationAssignedToRefOutParameter_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref a1, out a2);
        a1.Dispose();
        a2.Dispose();
        return;
    }
}
");
        }

        [Fact]
        public void Lambda_DisposableCreationAssignedToRefOutParameter_MultipleCalls_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
        myDelegate(ref /*2*/a1, out /*2*/a2);    // No diagnostics.
        a1.Dispose();
        a2.Dispose();
        return;
    }
}
",
            // Test0.cs(23,20): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(23, 20, "ref /*1*/a1"),
            // Test0.cs(23,33): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(23, 33, "out /*1*/a2"));
        }

        [Fact]
        public void Lambda_DisposableCreation_MultipleLevelsBelow_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    void M1()
    {
        MyDelegate myDelegate2 = (ref A param3, out A param4) =>
        {
            param3 = new A();
            param4 = new A();
        };

        MyDelegate myDelegate1 = (ref A param1, out A param2) =>
        {
            myDelegate2(ref /*2*/param1, out /*2*/param2);
        };

        A a1 = null, a2;
        myDelegate1(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
    }
}
",
            // Test0.cs(29,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(29, 21, "ref /*1*/a1"),
            // Test0.cs(29,34): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(29, 34, "out /*1*/a2"));
        }

        [Fact]
        public void Lambda_DisposableCreation_MultipleLevelsBelow_Nested_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    void M1()
    {
        MyDelegate myDelegate1 = (ref A param1, out A param2) =>
        {
            MyDelegate myDelegate2 = (ref A param3, out A param4) =>
            {
                param3 = new A();
                param4 = new A();
            };

            myDelegate2(ref /*2*/param1, out /*2*/param2);
        };

        A a1 = null, a2;
        myDelegate1(ref /*1*/a1, out /*1*/a2);    // This should fire two diagnostics.
    }
}
",
            // Test0.cs(29,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'ref /*1*/a1' before all references to it are out of scope.
            GetCSharpResultAt(29, 21, "ref /*1*/a1"),
            // Test0.cs(29,34): warning CA2000: Call System.IDisposable.Dispose on object created by 'out /*1*/a2' before all references to it are out of scope.
            GetCSharpResultAt(29, 34, "out /*1*/a2"));
        }

        [Fact]
        public void Lambda_InvokedFromInterprocedural_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a1 = new A();
        M2(() => a1.Dispose());
    }

    void M2(Action disposeCallback) => disposeCallback();
}
");
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void Lambda_MayBeInvokedFromInterprocedural_Diagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    public bool Flag;
    void M1()
    {
        A a1 = new A(1);
        M2(() => a1.Dispose());

        A a2 = new A(2);
        if (Flag)
            M3(() => a2.Dispose());
    }

    void M2(Action disposeCallback)
    {
        if (Flag)
            disposeCallback();
    }

    void M3(Action disposeCallback) => disposeCallback();
}
";
            var expected = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
            {
                expected = new[]
                {
                    // Test0.cs(17,16): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetCSharpMayBeNotDisposedResultAt(17, 16, "new A(1)"),
                    // Test0.cs(20,16): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetCSharpMayBeNotDisposedResultAt(20, 16, "new A(2)")
                };
            }

            VerifyCSharp(source, editorConfigFile, expected);
        }

        [Fact]
        public void DelegateInvocation_EmptyBody_NoArguments_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();

        System.Action myDelegate = M2;
        myDelegate();    // This should not change state of 'a' as it is not passed as argument.
    }

    void M2() { }
}
",
            // Test0.cs(17,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(17, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action = AddressOf M2
        myDelegate()      ' This should not change state of 'a' as it is not passed as argument.
    End Sub

    Sub M2()
    End Sub
End Module",
            // Test0.vb(14,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(14, 13, "New A()"));
        }

        [Fact]
        public void DelegateInvocation_PassedAsArgumentButNotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();

        System.Action<A> myDelegate = M2;
        myDelegate(a);    // This should not change state of 'a'.
    }

    void M2(A a) { }
}
",
            // Test0.cs(17,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(17, 13, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action(Of A) = AddressOf M2
        myDelegate(a)      ' This should not change state of 'a'.
    End Sub

    Sub M2(a As A)
    End Sub
End Module",
            // Test0.vb(14,13): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(14, 13, "New A()"));
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public void DelegateInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = new A();

        System.Action<A> myDelegate = M2;
        myDelegate(a);    // This should change state of 'a' to be disposed as we perform interprocedural analysis.
    }

    void M2(A a) => a.Dispose();
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action(Of A) = AddressOf M2
        myDelegate(a)      ' This should change state of 'a' to be disposed as we perform interprocedural analysis.
    End Sub

    Sub M2(a As A)
        a.Dispose()
    End Sub
End Module");
        }

        [Fact]
        public void PointsTo_ReferenceTypeCopyDisposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    public string Field;
    void M1(A a)
    {
        a = new A();
        A b = a;
        b.Dispose();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1(a As A)
        a = New A()
        Dim b As A = a
        b.Dispose()
    End Sub
End Class");
        }

        [Fact]
        public void DynamicObjectCreation_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i)
    {
    }
    public A(string s)
    {
    }

    public void Dispose()
    {

    }
}

class Test
{
    void M1(dynamic d)
    {
        A a = new A(d);
    }
}
",
            // Test0.cs(23,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(d)' before all references to it are out of scope.
            GetCSharpResultAt(23, 15, "new A(d)"));
        }

        [Fact]
        public void DynamicObjectCreation_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i)
    {
    }
    public A(string s)
    {
    }

    public void Dispose()
    {

    }
}

class Test
{
    void M1(dynamic d)
    {
        A a = new A(d);
        a.Dispose();
    }
}
");
        }

        [Fact]
        public void SpecialDisposableObjectCreationApis_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.IO;

class Test
{
    void M1(string filePath, FileMode fileMode)
    {
        var file = File.Open(filePath, fileMode);
        var file2 = File.CreateText(filePath);
    }
}
",
            // Test0.cs(9,20): warning CA2000: Call System.IDisposable.Dispose on object created by 'File.Open(filePath, fileMode)' before all references to it are out of scope.
            GetCSharpResultAt(9, 20, "File.Open(filePath, fileMode)"),
            // Test0.cs(10,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'File.CreateText(filePath)' before all references to it are out of scope.
            GetCSharpResultAt(10, 21, "File.CreateText(filePath)"));

            VerifyBasic(@"
Imports System
Imports System.IO

Class Test
    Private Sub M1(filePath As String, fileMode As FileMode)
        Dim f = File.Open(filePath, fileMode)
        Dim f2 = File.CreateText(filePath)
    End Sub
End Class
",
            // Test0.vb(7,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'File.Open(filePath, fileMode)' before all references to it are out of scope.
            GetBasicResultAt(7, 17, "File.Open(filePath, fileMode)"),
            // Test0.vb(8,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'File.CreateText(filePath)' before all references to it are out of scope.
            GetBasicResultAt(8, 18, "File.CreateText(filePath)"));
        }

        [Fact]
        public void SpecialDisposableObjectCreationApis_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.IO;

class Test
{
    void M1(string filePath, FileMode fileMode)
    {
        var file = File.Open(filePath, fileMode);
        file.Dispose();

        using (var file2 = File.CreateText(filePath))
        {
        }
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.IO

Class Test
    Private Sub M1(filePath As String, fileMode As FileMode)
        Dim f = File.Open(filePath, fileMode)
        f.Dispose()

        Using f2 = File.CreateText(filePath)
        End Using
    End Sub
End Class
");
        }

        [Fact]
        public void InvocationInstanceReceiverOrArgument_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }

    public void M()
    {
    }
}

class Test
{
    void M1(string param)
    {
        A a = new A();
        a.M();      // Invoking a method on disposable instance doesn't invalidate Dispose state.

        M2(a);      // Passing the disposable instance as an argument doesn't invalidate Dispose state.
    }

    void M2(A a)
    {
    }
}
",

            // Test0.cs(20,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(20, 15, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub M()
    End Sub
End Class

Class Test
    Private Sub M1(ByVal param As String)
        Dim a = New A()
        a.M()       ' Invoking a method on disposable instance doesn't invalidate Dispose state.

        M2(a)       ' Passing the disposable instance as an argument doesn't invalidate Dispose state.
    End Sub

    Public Sub M2(a As A)
    End Sub
End Class",
            // Test0.vb(16,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(16, 17, "New A()"));
        }

        [Fact]
        public void DisposableCreationInArgument_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        M2(new A());      // Passing the disposable instance as an argument doesn't invalidate Dispose state.
    }

    void M2(A a)
    {
    }
}
",

            // Test0.cs(16,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(16, 12, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Private Sub M1()
        M2(New A())       ' Passing the disposable instance as an argument doesn't invalidate Dispose state.
    End Sub

    Public Sub M2(a As A)
    End Sub
End Class",
            // Test0.vb(13,12): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(13, 12, "New A()"));
        }

        [Fact]
        public void DisposableCreationNotAssignedToAVariable_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public int X;
    public void Dispose()
    {

    }

    public void M()
    {
    }
}

class Test
{
    void M1()
    {
        new A();
        new A().M();
        var x = new A().X;
    }
}
",
            // Test0.cs(21,9): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(21, 9, "new A()"),
            // Test0.cs(22,9): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(22, 9, "new A()"),
            // Test0.cs(23,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(23, 17, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public X As Integer
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub M()
    End Sub
End Class

Class Test
    Private Sub M1()
        New A()
        New A().M()
        Dim x = New A().X
    End Sub
End Class", TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void DisposableCreationPassedToDisposableConstructor_NoDiagnostic()
        {
            // Dispose ownership transfer
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private readonly A _a;
    public B(A a)
    {
        _a = a;
    }

    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var b = new B(new A());
        b.Dispose();

        var a = new A();
        B b2 = null;
        try
        {
            b2 = new B(a);
        }
        finally
        {
            if (b2 != null)
            {
                b2.Dispose();
            }
        }

        var a2 = new A();
        B b3 = null;
        try
        {
            b3 = new B(a2);
        }
        finally
        {
            if (b3 != null)
            {
                b3.Dispose();
            }
        }
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable

    Public X As Integer
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private ReadOnly _a As A
    Public Sub New(ByVal a As A)
        _a = a
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Private Sub M1()
        Dim b = New B(New A())
        b.Dispose()
        Dim a = New A()
        Dim b2 As B = Nothing
        Try
            b2 = New B(a)
        Finally
            If b2 IsNot Nothing Then
                b2.Dispose()
            End If
        End Try

        Dim a2 = New A()
        Dim b3 As B = Nothing
        Try
            b3 = New B(a2)
        Finally
            If b3 IsNot Nothing Then
                b3.Dispose()
            End If
        End Try
    End Sub
End Class
");
        }

        [Fact]
        public void DisposableCreationPassedToDisposableConstructor_SpecialCases_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.IO;
using System.Resources;

class A : IDisposable
{
    private readonly object _a;
    public A(Stream a)
    {
        _a = a;
    }

    public A(TextReader t)
    {
        _a = t;
    }

    public A(TextWriter t)
    {
        _a = t;
    }

    public A(IResourceReader r)
    {
        _a = r;
    }

    public void Dispose()
    {
    }
}

class Test
{
    void M1(string filePath, FileMode fileMode)
    {
        Stream stream = new FileStream(filePath, fileMode);
        A a = null;
        try
        {
            a = new A(stream);
        }
        catch(IOException)
        {
            stream.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextReader reader = File.OpenText(filePath);
        a = null;
        try
        {
            a = new A(reader);
        }
        catch (IOException)
        {
            reader.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextWriter writer = File.CreateText(filePath);
        a = null;
        try
        {
            a = new A(writer);
        }
        catch (IOException)
        {
            writer.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        stream = new FileStream(filePath + filePath, fileMode);    // This is flagged as 'new ResourceReader(stream)' is not considered a Dispose ownership transfer, see https://github.com/dotnet/roslyn-analyzers/issues/1617
        ResourceReader resourceReader = null;
        a = null;
        try
        {
            resourceReader = new ResourceReader(stream);
            a = new A(resourceReader);
        }
        catch (IOException)
        {
            if (resourceReader != null)
            {
                resourceReader.Dispose();
            }
            else
            {
                stream.Dispose();
            }
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }
}
",
            // Test0.cs(92,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(92, 18, "new FileStream(filePath + filePath, fileMode)"));

            VerifyBasic(@"

Imports System
Imports System.IO
Imports System.Resources

Class A
    Implements IDisposable

    Private ReadOnly _a As Object
    Public Sub New(a As Stream)
        _a = a
    End Sub

    Public Sub New(t As TextReader)
        _a = t
    End Sub

    Public Sub New(t As TextWriter)
        _a = t
    End Sub

    Public Sub New(r As IResourceReader)
        _a = r
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test

    Private Sub M1(filePath As String, fileMode As FileMode)
        Dim stream As Stream = New FileStream(filePath, fileMode)
        Dim a As A = Nothing
        Try
            a = New A(stream)
        Catch ex As IOException
            stream.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim reader As TextReader = File.OpenText(filePath)
        a = Nothing
        Try
            a = New A(reader)
        Catch ex As IOException
            reader.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim writer As TextWriter = File.CreateText(filePath)
        a = Nothing
        Try
            a = New A(writer)
        Catch ex As IOException
            writer.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        stream = New FileStream(filePath + filePath, fileMode)  ' This is flagged as 'new ResourceReader(stream)' is not considered a Dispose ownership transfer, see https://github.com/dotnet/roslyn-analyzers/issues/1617
        Dim resourceReader As ResourceReader = Nothing
        a = Nothing
        Try
            resourceReader = New ResourceReader(stream)
            a = New A(resourceReader)
        Catch ex As IOException
            If resourceReader IsNot Nothing Then
                resourceReader.Dispose()
            Else
                stream.Dispose()
            End If

        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try
    End Sub
End Class
",
            // Test0.vb(70,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(70, 18, "New FileStream(filePath + filePath, fileMode)"));
        }

        [Fact, WorkItem(1580, "https://github.com/dotnet/roslyn-analyzers/issues/1580")]
        public void DisposableCreationPassedToDisposableConstructor_SpecialCases_ExceptionPath_Diagnostic()
        {
            // Disable interprocedural analysis to test special ctor invocation cases from metadata.
            var editorConfigFile = GetEditorConfigFileToDisableInterproceduralAnalysis(DisposeAnalysisKind.AllPaths);

            // For special dispose ownership transfer cases, we always assume ownership transfer
            // and the constructor invocation is assumed to be non-exception throwing.
            VerifyCSharp(@"
using System;
using System.IO;
using System.Resources;

class A : IDisposable
{
    public A(Stream a)
    {
    }

    public A(TextReader t)
    {
    }

    public A(TextWriter t)
    {
    }

    public A(IResourceReader r)
    {
    }

    public void Dispose()
    {

    }
}

class Test
{
    void M1(string filePath, FileMode fileMode)
    {
        Stream stream = new FileStream(filePath, fileMode);
        A a = null;
        try
        {
            a = new A(stream);
        }
        catch (IOException)
        {
            stream.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextReader reader = File.OpenText(filePath);
        a = null;
        try
        {
            a = new A(reader);
        }
        catch (IOException)
        {
            reader.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextWriter writer = File.CreateText(filePath);
        try
        {
            a = new A(writer);
        }
        catch (IOException)
        {
            writer.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        stream = new FileStream(filePath + filePath, fileMode);     // This is flagged as 'new ResourceReader(stream)' is not considered a Dispose ownership transfer, see https://github.com/dotnet/roslyn-analyzers/issues/1617
        ResourceReader resourceReader = null;
        a = null;
        try
        {
            resourceReader = new ResourceReader(stream);
            a = new A(resourceReader);
        }
        catch (IOException)
        {
            if (resourceReader != null)
            {
                resourceReader.Dispose();
            }
            else
            {
                stream.Dispose();
            }
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }
}
", editorConfigFile,
            // Test0.cs(34,25): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new FileStream(filePath, fileMode)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(34, 25, "new FileStream(filePath, fileMode)"),
            // Test0.cs(52,29): warning CA2000: Use recommended dispose pattern to ensure that object created by 'File.OpenText(filePath)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(52, 29, "File.OpenText(filePath)"),
            // Test0.cs(70,29): warning CA2000: Use recommended dispose pattern to ensure that object created by 'File.CreateText(filePath)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(70, 29, "File.CreateText(filePath)"),
            // Test0.cs(87,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(87, 18, "new FileStream(filePath + filePath, fileMode)"),
            // Test0.cs(92,30): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new ResourceReader(stream)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(92, 30, "new ResourceReader(stream)"));

            VerifyBasic(@"

Imports System
Imports System.IO
Imports System.Resources

Class A
    Implements IDisposable

    Public Sub New(a As Stream)
    End Sub

    Public Sub New(t As TextReader)
    End Sub

    Public Sub New(t As TextWriter)
    End Sub

    Public Sub New(r As IResourceReader)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test

    Private Sub M1(filePath As String, fileMode As FileMode)
        Dim stream As Stream = New FileStream(filePath, fileMode)
        Dim a As A = Nothing
        Try
            a = New A(stream)
        Catch ex As IOException
            stream.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim reader As TextReader = File.OpenText(filePath)
        a = Nothing
        Try
            a = New A(reader)
        Catch ex As IOException
            reader.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim writer As TextWriter = File.CreateText(filePath)
        a = Nothing
        Try
            a = New A(writer)
        Catch ex As IOException
            writer.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        stream = New FileStream(filePath + filePath, fileMode)
        Dim resourceReader As ResourceReader = Nothing
        a = Nothing
        Try
            resourceReader = New ResourceReader(stream)
            a = New A(resourceReader)
        Catch ex As IOException
            If resourceReader IsNot Nothing Then
                resourceReader.Dispose()
            Else
                stream.Dispose()
            End If

        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try
    End Sub
End Class
", editorConfigFile,
            // Test0.vb(30,32): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New FileStream(filePath, fileMode)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedOnExceptionPathsResultAt(30, 32, "New FileStream(filePath, fileMode)"),
            // Test0.vb(42,36): warning CA2000: Use recommended dispose pattern to ensure that object created by 'File.OpenText(filePath)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedOnExceptionPathsResultAt(42, 36, "File.OpenText(filePath)"),
            // Test0.vb(54,36): warning CA2000: Use recommended dispose pattern to ensure that object created by 'File.CreateText(filePath)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedOnExceptionPathsResultAt(54, 36, "File.CreateText(filePath)"),
            // Test0.vb(66,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(66, 18, "New FileStream(filePath + filePath, fileMode)"),
            // Test0.vb(70,30): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New ResourceReader(stream)' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedOnExceptionPathsResultAt(70, 30, "New ResourceReader(stream)"));
        }

        [Fact, WorkItem(1580, "https://github.com/dotnet/roslyn-analyzers/issues/1580")]
        public void DisposableCreationPassedToDisposableConstructor_SpecialCases_InterproceduralAnalysis_ExceptionPath_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.IO;
using System.Resources;

class A : IDisposable
{
    private object _a;
    public A(Stream a)
    {
        _a = a;
    }

    public A(TextReader t)
    {
        _a = t;
    }

    public A(TextWriter t)
    {
        _a = t;
    }

    public A(IResourceReader r)
    {
        _a = r;
    }

    public void Dispose()
    {

    }
}

class Test
{
    void M1(string filePath, FileMode fileMode)
    {
        Stream stream = new FileStream(filePath, fileMode);
        A a = null;
        try
        {
            a = new A(stream);
        }
        catch (IOException)
        {
            stream.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextReader reader = File.OpenText(filePath);
        a = null;
        try
        {
            a = new A(reader);
        }
        catch (IOException)
        {
            reader.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        TextWriter writer = File.CreateText(filePath);
        try
        {
            a = new A(writer);
        }
        catch (IOException)
        {
            writer.Dispose();
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }

        stream = new FileStream(filePath + filePath, fileMode);     // This is flagged as 'new ResourceReader(stream)' is not considered a Dispose ownership transfer, see https://github.com/dotnet/roslyn-analyzers/issues/1617
        ResourceReader resourceReader = null;
        a = null;
        try
        {
            resourceReader = new ResourceReader(stream);
            a = new A(resourceReader);
        }
        catch (IOException)
        {
            if (resourceReader != null)
            {
                resourceReader.Dispose();
            }
            else
            {
                stream.Dispose();
            }
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }
}
",
            // Test0.cs(92,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(92, 18, "new FileStream(filePath + filePath, fileMode)"));

            VerifyBasic(@"

Imports System
Imports System.IO
Imports System.Resources

Class A
    Implements IDisposable

    Private _a As Object
    Public Sub New(a As Stream)
        _a = a
    End Sub

    Public Sub New(t As TextReader)
        _a = t
    End Sub

    Public Sub New(t As TextWriter)
        _a = t
    End Sub

    Public Sub New(r As IResourceReader)
        _a = r
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test

    Private Sub M1(filePath As String, fileMode As FileMode)
        Dim stream As Stream = New FileStream(filePath, fileMode)
        Dim a As A = Nothing
        Try
            a = New A(stream)
        Catch ex As IOException
            stream.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim reader As TextReader = File.OpenText(filePath)
        a = Nothing
        Try
            a = New A(reader)
        Catch ex As IOException
            reader.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        Dim writer As TextWriter = File.CreateText(filePath)
        a = Nothing
        Try
            a = New A(writer)
        Catch ex As IOException
            writer.Dispose()
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try

        stream = New FileStream(filePath + filePath, fileMode)
        Dim resourceReader As ResourceReader = Nothing
        a = Nothing
        Try
            resourceReader = New ResourceReader(stream)
            a = New A(resourceReader)
        Catch ex As IOException
            If resourceReader IsNot Nothing Then
                resourceReader.Dispose()
            Else
                stream.Dispose()
            End If

        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try
    End Sub
End Class
",
            // Test0.vb(71,18): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New FileStream(filePath + filePath, fileMode)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(71, 18, "New FileStream(filePath + filePath, fileMode)"));
        }

        [Theory, WorkItem(1580, "https://github.com/dotnet/roslyn-analyzers/issues/1580")]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void DisposableObjectNotDisposed_ExceptionPath_Diagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            var source = @"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        var a = new A();
        ThrowException();   // a not disposed on exception path.
        a.Dispose();
    }

    void M2()
    {
        var a = new A();
        try
        {
            ThrowException();
            a.Dispose();
        }
        catch (Exception)
        {
            // a not disposed on this path.
        }
    }

    void M3()
    {
        var a = new A();
        try
        {
            ThrowException();
            a.Dispose();
        }
        catch (System.IO.IOException)
        {
            a.Dispose();
            // a not disposed on path with other exceptions.
        }
    }

    void ThrowException()
    {
        throw new NotImplementedException();
    }
}";
            var builder = new List<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                builder.AddRange(new[]
                {
                    // Test0.cs(16,17): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(16, 17, "new A()"),
                    // Test0.cs(23,17): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(23, 17, "new A()"),
                    // Test0.cs(37,17): warning CA2000: Object created by 'new A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetCSharpNotDisposedOnExceptionPathsResultAt(37, 17, "new A()")
                });
            }
            else if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
            {
                builder.Add(
                    // Test0.cs(23,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A()' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetCSharpMayBeNotDisposedResultAt(23, 17, "new A()"));
            }

            VerifyCSharp(source, editorConfigFile, builder.ToArray());

            source = @"
Imports System

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test

    Private Sub M1()
        Dim a = New A()
        ThrowException()    ' a not disposed on exception path.
        a.Dispose()
    End Sub

    Private Sub M2()
        Dim a = New A()
        Try
            ThrowException()
            a.Dispose()
        Catch ex As Exception
            ' a not disposed on this path.
        End Try
    End Sub

    Private Sub M3()
        Dim a = New A()
        Try
            ThrowException()
            a.Dispose()
        Catch ex As System.IO.IOException
            a.Dispose()
            ' a not disposed on path with other exceptions.
        End Try
    End Sub

    Private Sub ThrowException()
        Throw New NotImplementedException()
    End Sub
End Class
";
            builder.Clear();
            if (disposeAnalysisKind.AreExceptionPathsEnabled())
            {
                builder.AddRange(new[]
                {
                    // Test0.vb(15,17): warning CA2000: Object created by 'New A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(15, 17, "New A()"),
                    // Test0.vb(31,17): warning CA2000: warning CA2000: Object created by 'New A()' is not disposed along all exception paths. Call System.IDisposable.Dispose on the object before all references to it are out of scope.
                    GetBasicNotDisposedOnExceptionPathsResultAt(31, 17, "New A()")
                });
            }

            if (disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled())
            {
                var index = builder.Count == 0 ? 0 : 1;
                builder.Insert(index,
                    // Test0.vb(21,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A()' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetBasicMayBeNotDisposedResultAt(21, 17, "New A()"));
            }

            VerifyBasic(source, editorConfigFile, builder.ToArray());
        }

        [Fact]
        public void DisposableObjectOnlyDisposedOnExceptionPath_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (Exception)
        {
            a.Dispose();
        }
    }

    void M2()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            a.Dispose();
        }
    }

    void M3()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            a.Dispose();
        }
        catch (Exception)
        {
            a.Dispose();
        }
    }

    void M4(bool flag)
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            if (flag)
            {
                a.Dispose();
            }
        }
    }

    void ThrowException()
    {
        throw new NotImplementedException();
    }
}",
            // Test0.cs(15,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(15, 17, "new A()"),
            // Test0.cs(28,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(28, 17, "new A()"),
            // Test0.cs(41,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(41, 17, "new A()"),
            // Test0.cs(58,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(58, 17, "new A()"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test

    Private Sub M1()
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As Exception
            a.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            a.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            a.Dispose()
        Catch ex As Exception
            a.Dispose()
        End Try
    End Sub

    Private Sub M4(flag As Boolean)
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            If flag Then
                a.Dispose()
            End If
        End Try
    End Sub

    Private Sub ThrowException()
        Throw New NotImplementedException()
    End Sub
End Class
",
            // Test0.vb(14,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(14, 17, "New A()"),
            // Test0.vb(23,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(23, 17, "New A()"),
            // Test0.vb(32,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(32, 17, "New A()"),
            // Test0.vb(43,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(43, 17, "New A()"));
        }

        [Fact]
        public void DisposableObjectDisposed_FinallyPath_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        finally
        {
            a.Dispose();
        }
    }

    void M2()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (Exception)
        {
        }
        finally
        {
            a.Dispose();
        }
    }

    void M3()
    {
        var a = new A();
        try
        {
            ThrowException();   
            a.Dispose();
            a = null;
        }
        catch (System.IO.IOException)
        {
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }

    void ThrowException()
    {
        throw new NotImplementedException();
    }
}");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test

    Private Sub M1()
        Dim a = New A()
        Try
            ThrowException()
        Finally
            a.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As Exception
        Finally
            a.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a = New A()
        Try
            ThrowException()
            a.Dispose()
            a = Nothing
        Catch ex As System.IO.IOException
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try
    End Sub

    Private Sub ThrowException()
        Throw New NotImplementedException()
    End Sub
End Class
");
        }

        [Fact, WorkItem(1597, "https://github.com/dotnet/roslyn-analyzers/issues/1597")]
        public void DisposableObjectInErrorCode_NotDisposed_BailOut_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : IDisposable
{
    public void Dispose()
    {
        A x = new A();
        = x
    }
}
", TestValidationMode.AllowCompileErrors);
        }

        [Fact, WorkItem(1597, "https://github.com/dotnet/roslyn-analyzers/issues/1597")]
        public void DisposableObjectInErrorCode_02_NotDisposed_BailOut_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.IO;
using System.Text;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        var builder = new StringBuilder();
        using ()        // This erroneous code used to cause a null reference exception in the analysis.
        this.WriteTo(new StringWriter(builder));
        return;
    }

    void WriteTo(StringWriter x)
    {
    }
}
", TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void DelegateCreation_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1()
    {
        Func<A> createA = M2;
        A a = createA();
        a.Dispose();
    }

    A M2()
    {
        return new A();
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        Dim createA As Func(Of A) = AddressOf M2
        Dim a As A = createA()
        a.Dispose()
    End Sub

    Function M2() As A
        Return New A()
    End Function
End Class");
        }

        [Fact, WorkItem(1602, "https://github.com/dotnet/roslyn-analyzers/issues/1602")]
        public void MemberReferenceInQueryFromClause_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Immutable;
using System.Linq;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B: IDisposable
{
    public C C { get; }
    public void Dispose()
    {

    }
}

class C
{
    public ImmutableArray<A> ArrayOfA { get; }
}

class Test
{
    void M1(ImmutableArray<B> arrayOfB)
    {
        var x = from b in arrayOfB
            from a in b.C.ArrayOfA
            select a;
        var y = new A();
        y.Dispose();
    }
}
");
        }

        [Fact]
        public void SystemThreadingTask_SpecialCase_NotDisposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System.Threading.Tasks;

public class A
{
    void M()
    {
        Task t = new Task(null);
        M1(out var t2);
    }

    void M1(out Task<int> t)
    {
        t = null;
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Public Class A

    Private Sub M()
        Dim t As Task = New Task(Nothing)
        Dim t2 As Task = Nothing
        M1(t2)
    End Sub

    Private Sub M1(<Out> ByRef t As Task(Of Integer))
        t = Nothing
    End Sub
End Class");
        }

        [Fact]
        public void MultipleReturnStatements_AllInstancesReturned_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    A M1(bool flag)
    {
        A a;
        if (flag)
        {
            A a2 = new A();
            a = a2;
            return a;
        }

        A a3 = new A();
        a = a3;
        return a;
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Function M1(ByVal flag As Boolean) As A
        Dim a As A
        If flag Then
            Dim a2 As New A()
            a = a2
            Return a
        End If

        Dim a3 As New A()
        a = a3
        Return a
    End Function
End Class
");
        }

        [Fact]
        public void MultipleReturnStatements_AllInstancesEscapedWithOutParameter_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    void M1(bool flag, out A a)
    {
        if (flag)
        {
            A a2 = new A();
            a = a2;
            return;
        }

        A a3 = new A();
        a = a3;
        return;
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Sub M1(ByVal flag As Boolean, <Out> ByRef a As A)
        If flag Then
            Dim a2 As New A()
            a = a2
            Return
        End If

        Dim a3 As New A()
        a = a3
        Return
    End Sub
End Class
");
        }

        [Fact]
        public void MultipleReturnStatements_AllButOneInstanceReturned_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

public class Test
{
    A M1(int flag, bool flag2, bool flag3)
    {
        A a = null;
        if (flag == 0)
        {
            A a2 = new A(1);        // Escaped with return inside below nested 'if', not disposed on other paths.
            a = a2;

            if (!flag2)
            {
                if (flag3)
                {
                    return a;
                }
            }
        }
        else
        {
            a = new A(2);        // Escaped with return inside below nested 'else', not disposed on other paths.
            if (flag == 1)
            {
                a = new A(3);    // Never disposed.
            }
            else
            {
                if (flag3)
                {
                    a = new A(4);    // Escaped with return inside below 'else', not disposed on other paths.
                }

                if (flag2)
                {
                }
                else
                {
                    return a;
                }
            }
        }

        A a3 = new A(5);     // Always escaped with below return, ensure no diagnostic.
        a = a3;
        return a;
    }
}
",
            // Test0.cs(20,20): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(20, 20, "new A(1)"),
            // Test0.cs(33,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(33, 17, "new A(2)"),
            // Test0.cs(36,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(36, 21, "new A(3)"),
            // Test0.cs(42,25): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(4)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(42, 25, "new A(4)"));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Function M1(flag As Integer, flag2 As Boolean, flag3 As Boolean) As A
        Dim a As A = Nothing
        If flag = 0 Then
            Dim a2 As A = New A(1)   ' Escaped with return inside below nested 'if', not disposed on other paths.
            a = a2
            If Not flag2 Then
                If flag3 Then
                    Return a
                End If
            End If
        Else
            a = New A(2)     ' Escaped with return inside below nested 'else', not disposed on other paths.
            If flag = 1 Then
                a = New A(3)     ' Never disposed on any path.
            Else
                If flag3 Then
                    a = New A(4)     ' Escaped with return inside below 'else', not disposed on other paths.
                End If

                If flag2 Then
                Else
                    Return a
                End If
            End If
        End If

        Dim a3 As A = New A(5)     ' Always escaped with below return, ensure no diagnostic.
        a = a3
        Return a
    End Function
End Class
",
            // Test0.vb(21,27): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(21, 27, "New A(1)"),
            // Test0.vb(29,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(29, 17, "New A(2)"),
            // Test0.vb(31,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A(3)' before all references to it are out of scope.
            GetBasicResultAt(31, 21, "New A(3)"),
            // Test0.vb(34,25): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(4)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(34, 25, "New A(4)"));
        }

        [Fact]
        public void MultipleReturnStatements_AllButOneInstanceEscapedWithOutParameter_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class B : A
{
}

public class Test
{
    void M1(int flag, bool flag2, bool flag3, out A a)
    {
        a = null;
        if (flag == 0)
        {
            A a2 = new A();        // Escaped with return inside below nested 'if'.
            a = a2;

            if (!flag2)
            {
                if (flag3)
                {
                    return;
                }
            }
        }
        else
        {
            a = new A();        // Escaped with return inside below nested 'else'.
            if (flag == 1)
            {
                a = new B();    // Never disposed.
            }
            else
            {
                if (flag3)
                {
                    a = new A();    // Escaped with return inside below 'else'.
                }

                if (flag2)
                {
                }
                else
                {
                    return;
                }
            }
        }

        A a3 = new A();     // Escaped with below return.
        a = a3;
        return;
    }
}
",
            // Test0.cs(39,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new B()' before all references to it are out of scope.
            GetCSharpResultAt(39, 21, "new B()"));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Public Class Test
    Private Sub M1(flag As Integer, flag2 As Boolean, flag3 As Boolean, <Out> ByRef a As A)
        a = Nothing
        If flag = 0 Then
            Dim a2 As A = New A()   ' Escaped with return inside below nested 'if'.
            a = a2
            If Not flag2 Then
                If flag3 Then
                    Return
                End If
            End If
        Else
            a = New A()     ' Escaped with return inside below nested 'else'.
            If flag = 1 Then
                a = New B()     ' Never disposed
            Else
                If flag3 Then
                    a = New A()     ' Escaped with return inside below 'else'.
                End If

                If flag2 Then
                Else
                    Return
                End If
            End If
        End If

        Dim a3 As A = New A()     ' Escaped with below return.
        a = a3
        Return
    End Sub
End Class
",
            // Test0.vb(31,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'New B()' before all references to it are out of scope.
            GetBasicResultAt(31, 21, "New B()"));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AssignedToTuple_Escaped_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    (A, int) M1()
    {
        A a = new A();
        return (a, 0);
    }

    (A, int) M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
        return b;
    }
}
");

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Function M1() As (a As A, i As Integer)
        Dim a As A = New A()
        Return (a, 0)
    End Function

    Private Function M2() As (a As A, i As Integer)
        Dim a As A = New A()
        Dim b As (a As A, i As Integer) = (a, 0)
        Return b
    End Function
End Class
");
        }

        [Theory, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void DisposableAllocation_AssignedToTuple_Escaped_SpecialCases_NoDiagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var editorConfigFile = GetEditorConfigFile(disposeAnalysisKind);

            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    // Nested tuple
    ((A, int), int) M1()
    {
        A a = new A();
        ((A, int), int) b = ((a, 0), 1);
        return b;
    }

    // Declaration expression target
    A M2()
    {
        A a = new A();
        var ((a2, x), y) = ((a, 0), 1);
        return a2;
    }

    // Declaration expression target with discards
    A M3()
    {
        A a = new A();
        var ((a2, _), _) = ((a, 0), 1);
        return a2;
    }

    // Declaration expressions in target
    A M4()
    {
        A a = new A();
        ((var a2, var x), var y) = ((a, 0), 1);
        return a2;
    }

    // Discards in target
    A M5()
    {
        A a = new A();
        ((var a2, _), _) = ((a, 0), 1);
        return a2;
    }

    // Tuple with multiple disposable escape
    (A, A) M6()
    {
        A a = new A();
        A a2 = new A();
        var b = (a, a2);
        return b;
    }
}
", editorConfigFile);
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AssignedToTuple_NotDisposed_SpecialCases_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }

    public void Dispose()
    {

    }
}

public class Test
{
    // Nested tuple
    ((A, int), (A, int)) M1()
    {
        A a = new A(1);     // Should be flagged.
        A a2 = new A(2);
        ((A, int), (A, int)) b = ((a2, 0), (a2, 0));
        return b;
    }

    // Declaration expression target
    A M2()
    {
        A a = new A(3);     // Should be flagged.
        var ((a2, x), y) = ((a, 0), 1);
        return null;
    }

    // Declaration expression target with discards
    A M3()
    {
        A a = new A(4);     // Should be flagged.
        var ((a2, _), _) = ((a, 0), 1);
        return null;
    }

    // Declaration expressions in target
    A M4()
    {
        A a = new A(5);     // Should be flagged.
        ((var a2, var x), var y) = ((a, 0), 1);
        return null;
    }

    // Discards in target
    A M5()
    {
        A a = new A(6);     // Should be flagged.
        ((var a2, _), _) = ((a, 0), 1);
        return null;
    }

    // Tuple with multiple disposable escape
    (A, A) M6()
    {
        A a = new A(7);     // Should be flagged.
        A a2 = new A(8);
        var b = (a2, a2);
        return b;
    }
}
",
            // Test0.cs(19,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(19, 15, "new A(1)"),
            // Test0.cs(28,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(28, 15, "new A(3)"),
            // Test0.cs(36,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(4)' before all references to it are out of scope.
            GetCSharpResultAt(36, 15, "new A(4)"),
            // Test0.cs(44,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(5)' before all references to it are out of scope.
            GetCSharpResultAt(44, 15, "new A(5)"),
            // Test0.cs(52,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(6)' before all references to it are out of scope.
            GetCSharpResultAt(52, 15, "new A(6)"),
            // Test0.cs(60,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(7)' before all references to it are out of scope.
            GetCSharpResultAt(60, 15, "new A(7)"));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_EscapedTupleLiteral_SpecialCases_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    // Tuple literal escaped cases.
    ((A, int), int) M1()
    {
        A a = new A();
        return ((a, 0), 1);
    }

    (A, A) M2()
    {
        A a = new A();
        A a2 = new A();
        return (a, a2);
    }

    void M3(out (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        arg = (a, a2);
    }

    void M4(out (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        var a3 = (a, a2);
        arg = a3;
    }

    void M5(ref (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        var a3 = (a, a2);
        arg = a3;
    }
}
");
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AddedToTupleLiteral_SpecialCases_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {

    }
}

public class Test
{
    // Tuple literal assignment cases.
    void M1()
    {
        A a = new A(1);
        var x = ((a, 0), 1);
    }

    void M2()
    {
        A a = new A(2);
        A a2 = new A(3);
        var x = (a, a2);
    }

    void M3(out (A, A) arg)
    {
        A a = new A(4);
        A a2 = new A(5);
        arg = (a, a2);
        arg = default((A, A));
    }

    void M4(out (A, A) arg)
    {
        A a = new A(6);
        A a2 = new A(7);
        var a3 = (a, a2);
        arg = a3;
        arg = default((A, A));
    }
}
",
            // Test0.cs(18,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(18, 15, "new A(1)"),
            // Test0.cs(24,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(24, 15, "new A(2)"),
            // Test0.cs(25,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(25, 16, "new A(3)"),
            // Test0.cs(31,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(4)' before all references to it are out of scope.
            GetCSharpResultAt(31, 15, "new A(4)"),
            // Test0.cs(32,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(5)' before all references to it are out of scope.
            GetCSharpResultAt(32, 16, "new A(5)"),
            // Test0.cs(39,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(6)' before all references to it are out of scope.
            GetCSharpResultAt(39, 15, "new A(6)"),
            // Test0.cs(40,16): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(7)' before all references to it are out of scope.
            GetCSharpResultAt(40, 16, "new A(7)"));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AssignedToTuple_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    void M1()
    {
        A a = new A();
        var b = (a, 0);
    }

    void M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
    }
}",
            // Test0.cs(16,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(16, 15, "new A()"),
            // Test0.cs(22,15): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(22, 15, "new A()"));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Sub M1()
        Dim a As A = New A()
        Dim b = (a, 0)
    End Sub

    Private Sub M2()
        Dim a As A = New A()
        Dim b As (a As A, i As Integer) = (a, 0)
    End Sub
End Class
",
            // Test0.vb(15,22): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(15, 22, "New A()"),
            // Test0.vb(20,22): warning CA2000: Call System.IDisposable.Dispose on object created by 'New A()' before all references to it are out of scope.
            GetBasicResultAt(20, 22, "New A()"));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AssignedToTuple_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    void M1()
    {
        A a = new A();
        var b = (a, 0);
        b.a.Dispose();
    }

    void M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
        a.Dispose();
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(CSharpLanguageVersion.CSharp7_3));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Sub M1()
        Dim a As A = New A()
        Dim b = (a, 0)
        b.a.Dispose()
    End Sub

    Private Sub M2()
        Dim a As A = New A()
        Dim b As (a As A, i As Integer) = (a, 0)
        a.Dispose()
    End Sub
End Class
", parseOptions: VisualBasicParseOptions.Default.WithLanguageVersion(VisualBasicLanguageVersion.VisualBasic15_3));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_AssignedToTuple_Item1_Disposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    void M1()
    {
        A a = new A();
        var b = (a, 0);
        b.Item1.Dispose();
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(CSharpLanguageVersion.CSharp7_3));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Public Class Test
    Private Sub M1()
        Dim a As A = New A()
        Dim b = (a, 0)
        b.Item1.Dispose()
    End Sub
End Class
", parseOptions: VisualBasicParseOptions.Default.WithLanguageVersion(VisualBasicLanguageVersion.VisualBasic15_3));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_DeconstructionAssignmentToTuple_DeconstructMethod_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

internal static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}

class A : IDisposable
{
    public A(int i) { }
    public int X { get; }
    public void Dispose()
    {
    }

    public int M() => 0;
}

public class Test
{
    void M1(IDictionary<A, int> map)
    {
        foreach ((A a, _) in map)
        {
            var x = new A(1);
            var y = a.M();
        }
    }

    void M2(IDictionary<A, int> map)
    {
        foreach (var (a, _) in map)
        {
            var x = new A(2);
            var y = a.M();
        }
    }

    void M3(KeyValuePair<A, int> pair, int y)
    {
        A a;
        (a, y) = pair;
        var x = new A(3);
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(CSharpLanguageVersion.CSharp7_3),
    expected: new[] {
            // Test0.cs(31,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(31, 21, "new A(1)"),
            // Test0.cs(40,21): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(2)' before all references to it are out of scope.
            GetCSharpResultAt(40, 21, "new A(2)"),
            // Test0.cs(49,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(3)' before all references to it are out of scope.
            GetCSharpResultAt(49, 17, "new A(3)")
    });
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public void DisposableAllocation_RefArgument_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

public class Test
{
    private Test _field;
    public void M1()
    {
        Interlocked.CompareExchange(ref _field, null, new Test());
        var a = new A(1);
    }
}",
            // Test0.cs(19,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(1)' before all references to it are out of scope.
            GetCSharpResultAt(19, 17, "new A(1)"));
        }

        [Fact]
        public void DisposableAllocation_IncrementOperator_RegressionTest()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

public class Test
{
    private int i;
    void M()
    {
        var a = new A();
        i++;
        a.Dispose();
    }
}
");
        }

        [Fact]
        public void DifferentDisposePatternsInFinally_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        // Allocated before try, disposed in finally with conditional access.
        A a = new A(1);
        try
        {
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M2()
    {
        // Allocated in try, disposed in finally with conditional access.
        A a = null;
        try
        {
            a = new A(2);
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M3()
    {
        // Allocated before try, disposed in finally with null check.
        A a = new A(3);
        try
        {
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }

    void M4()
    {
        // Allocated in try, disposed in finally with null check.
        A a = null;
        try
        {
            a = new A(4);
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }

    void M5()
    {
        // Allocated before try, disposed in finally with helper method.
        A a = new A(5);
        try
        {
        }
        finally
        {
            DisposeHelper(a);
        }
    }

    void M6()
    {
        // Allocated in try, disposed in finally with helper method.
        A a = null;
        try
        {
            a = new A(6);
        }
        finally
        {
            DisposeHelper(a);
        }
    }

    void DisposeHelper(IDisposable a)
    {
        if (a != null)
        {
            a?.Dispose();
        }
    }

    void M7(bool flag)
    {
        // Allocated before try, disposed in try and assigned to null, disposed in finally with conditional access.
        A a = new A(7);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M8(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, disposed in try and assigned to null, disposed in finally with conditional access.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(8);
            }

            if (flag2)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M9(bool flag)
    {
        // Allocated before try, disposed in catch and all exit points from try, but not in finally.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            a.Dispose();
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    void M10(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, disposed in catch and all exit points from try, but not in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                a?.Dispose();
                return;
            }

            if (a != null)
            {
                a.Dispose();
            }
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    private IDisposable A;
    void M11(bool flag)
    {
        // Allocated before try, escaped or disposed at all exit points from try, and disposed with conditional access in finally.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            this.A = a;     // Escaped.
            a = null;
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M12(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, escaped or disposed at all exit points from try, and disposed with conditional access in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                this.A = a;     // Escaped.
                a = null;
                return;
            }

            if (a != null)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }
}
");

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Private Sub M1()
        Dim a As A = New A(1)

        Try
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a As A = Nothing

        Try
            a = New A(2)
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a As A = New A(3)

        Try
        Finally
            If a IsNot Nothing Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M4()
        Dim a As A = Nothing

        Try
            a = New A(4)
        Finally
            If a IsNot Nothing Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M5()
        Dim a As A = New A(5)

        Try
        Finally
            DisposeHelper(a)
        End Try
    End Sub

    Private Sub M6()
        Dim a As A = Nothing

        Try
            a = New A(6)
        Finally
            DisposeHelper(a)
        End Try
    End Sub

    Private Sub DisposeHelper(a As IDisposable)
        If a IsNot Nothing Then
            a?.Dispose()
        End If
    End Sub

    Private Sub M7(flag As Boolean)
        Dim a As A = New A(7)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M8(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(8)
            End If

            If flag2 Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M9(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            a.Dispose()
        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private Sub M10(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                a?.Dispose()
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
            End If

        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private A As IDisposable

    Private Sub M11(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            Me.A = a
            a = Nothing
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M12(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                Me.A = a
                a = Nothing
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub
End Class
");
        }

        [Fact]
        public void DifferentDisposePatternsInFinally_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1(bool flag)
    {
        // Allocated before try, disposed only on some paths in finally with conditional access.
        A a = new A(1);
        try
        {
        }
        finally
        {
            if (flag)
            {
                a?.Dispose();
            }
        }
    }

    void M2(bool flag)
    {
        // Allocated in try, disposed only on some paths in finally with conditional access.
        A a = null;
        try
        {
            a = new A(2);
        }
        finally
        {
            if (flag)
            {
                a?.Dispose();
            }
        }
    }

    void M3(bool flag)
    {
        // Allocated before try, disposed in finally with null checks on different variable.
        // It is not recommended to have dispose logic of a variable depend on multiple variables/flags, as the
        // lifetime of allocations might change when code within the try is refactored.
        A a = null;
        A b = null;
        try
        {
            if (flag)
            {
                a = new A(3);
                b = new A(31);
            }
        }
        finally
        {
            if (b != null)
            {
                a.Dispose();
                b.Dispose();
            }
        }
    }

    void M4(bool flag)
    {
        // Allocated in try, disposed in finally with null checks on multiple variables.
        // It is not recommended to have dispose logic of a variable depend on another variable, as the
        // lifetime of allocations might change when code within the try is refactored.
        A a = null;
        A b = null;
        try
        {
            if (flag)
            {
                a = new A(4);
                b = new A(41);
            }
        }
        finally
        {
            if (a != null && b != null)
            {
                a.Dispose();
                b.Dispose();
            }
        }
    }

    void M5(bool flag)
    {
        // Allocated before try, disposed on some paths in finally with helper method.
        A a = new A(5);
        try
        {
        }
        finally
        {
            DisposeHelper(a, flag);
        }
    }

    void M6(bool flag)
    {
        // Allocated in try, disposed in finally with helper method depending on a bool check.
        // It is not recommended to have dispose logic of a variable depend on another flag, as the
        // lifetime of allocation and flag value might change when code within the try is refactored.
        A a = null;
        try
        {
            if (flag)
            {
                a = new A(6);
            }
        }
        finally
        {
            DisposeHelper(a, flag);
        }
    }

    void DisposeHelper(IDisposable a, bool flag)
    {
        if (flag)
        {
            a?.Dispose();
        }
    }

    void M7(bool flag)
    {
        // Allocated before try, leaked on some paths in try, disposed in finally with conditional access.
        A a = new A(7);
        try
        {
            if (flag)
            {
                a = null;   // Leaked here, but need path sensitive analysis to flag this.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M8(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, leaked on some paths in try, disposed in finally with conditional access.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(8);
            }

            if (flag2)
            {
                a.Dispose();
                a = null;
            }
            else
            {
                a = null;   // Leaked here, needs path sensitive analysis.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M9(bool flag)
    {
        // Allocated before try, disposed in catch and but leaked from some exit points in try.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a = null;   // Leaked here.
                return;
            }

            a.Dispose();
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    void M10(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, leaked from some exit points in catch.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                a?.Dispose();
                return;
            }

            if (a != null)
            {
                a.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (flag1)
            {
                a?.Dispose();   // Leaked here, but need enhanced exceptional path dispose analysis to flag this.
            }
        }
        finally
        {
        }
    }

    private IDisposable A;
    void M11(bool flag)
    {
        // Allocated before try, leaked before escaped at some points in try.
        A a = new A(11);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            a = null;       // Leaked here.
            this.A = a;     // Escaped has no effect as it is already leaked.
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M12(bool flag1, bool flag2, bool flag3)
    {
        // Conditionally allocated in try, escaped and leaked on separate exit points from try, and disposed with conditional access in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(12);
            }

            if (flag2)
            {
                this.A = a;     // Escaped.
                a = null;
                return;
            }
            else if (flag3)
            {
                a = new A(121);   // Previous allocation potentially leaked here, but need path sensitive analysis to flag here.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }
}
", GetEditorConfigFile(DisposeAnalysisKind.AllPaths),
            // Test0.cs(17,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(17, 15, "new A(1)"),
            // Test0.cs(36,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(36, 17, "new A(2)"),
            // Test0.cs(58,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(3)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(58, 21, "new A(3)"),
            // Test0.cs(83,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(4)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(83, 21, "new A(4)"),
            // Test0.cs(84,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(41)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(84, 21, "new A(41)"),
            // Test0.cs(100,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(5)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(100, 15, "new A(5)"),
            // Test0.cs(120,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(6)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(120, 21, "new A(6)"),
            // Test0.cs(184,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(9)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(184, 15, "new A(9)"),
            // Test0.cs(242,15): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A(11)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetCSharpMayBeNotDisposedResultAt(242, 15, "new A(11)"));

            VerifyBasic(@"
Imports System

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Private Sub M1(flag As Boolean)
        Dim a As A = New A(1)

        Try
        Finally
            If flag Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M2(flag As Boolean)
        Dim a As A = Nothing

        Try
            a = New A(2)
        Finally
            If flag Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M3(flag As Boolean)
        Dim a As A = Nothing
        Dim b As A = Nothing

        Try
            If flag Then
                a = New A(3)
                b = New A(31)
            End If
        Finally
            If b IsNot Nothing Then
                a.Dispose()
                b.Dispose()
            End If
        End Try
    End Sub

    Private Sub M4(flag As Boolean)
        Dim a As A = Nothing
        Dim b As A = Nothing

        Try
            If flag Then
                a = New A(4)
                b = New A(41)
            End If
        Finally
            If a IsNot Nothing AndAlso b IsNot Nothing Then
                a.Dispose()
                b.Dispose()
            End If
        End Try
    End Sub

    Private Sub M5(flag As Boolean)
        Dim a As A = New A(5)

        Try
        Finally
            DisposeHelper(a, flag)
        End Try
    End Sub

    Private Sub M6(flag As Boolean)
        Dim a As A = Nothing

        Try
            If flag Then
                a = New A(6)
            End If
        Finally
            DisposeHelper(a, flag)
        End Try
    End Sub

    Private Sub DisposeHelper(a As IDisposable, flag As Boolean)
        If flag Then
            a?.Dispose()
        End If
    End Sub

    Private Sub M7(flag As Boolean)
        Dim a As A = New A(7)

        Try
            If flag Then
                a = Nothing
            End If
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M8(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(8)
            End If

            If flag2 Then
                a.Dispose()
                a = Nothing
            Else
                a = Nothing
            End If
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M9(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a = Nothing
                Return
            End If

            a.Dispose()
        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private Sub M10(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                a?.Dispose()
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
            End If
        Catch ex As Exception
            If flag1 Then
                a?.Dispose()
            End If

        Finally
        End Try
    End Sub

    Private A As IDisposable

    Private Sub M11(flag As Boolean)
        Dim a As A = New A(11)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            a = Nothing
            Me.A = a
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M12(flag1 As Boolean, flag2 As Boolean, flag3 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(12)
            End If

            If flag2 Then
                Me.A = a
                a = Nothing
                Return
            ElseIf flag3 Then
                a = New A(121)
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub
End Class
", GetEditorConfigFile(DisposeAnalysisKind.AllPaths),
            // Test0.vb(16,22): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(1)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(16, 22, "New A(1)"),
            // Test0.vb(30,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(2)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(30, 17, "New A(2)"),
            // Test0.vb(44,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(3)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(44, 21, "New A(3)"),
            // Test0.vb(61,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(4)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(61, 21, "New A(4)"),
            // Test0.vb(62,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(41)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(62, 21, "New A(41)"),
            // Test0.vb(73,22): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(5)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(73, 22, "New A(5)"),
            // Test0.vb(86,21): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(6)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(86, 21, "New A(6)"),
            // Test0.vb(131,22): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(9)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(131, 22, "New A(9)"),
            // Test0.vb(174,22): warning CA2000: Use recommended dispose pattern to ensure that object created by 'New A(11)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
            GetBasicMayBeNotDisposedResultAt(174, 22, "New A(11)"));
        }

        [Fact]
        public void DisposableObjectsCopyValues_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B
{
    private readonly A _a;
    public B(A a1, A a2)
    {
        _a = a1;
    }

    public void M2(A param2)
    {
        if (param2 == null)
        {
            throw new ArgumentNullException(nameof(param2));
        }
    }
}

class Test
{
    void M1(A param1)
    {
        using (var a = new A())
        {
            var b = new B(a, param1);
            b.M2(param1);
        }
    }
}
");
        }

        [Fact]
        public void DisposableObjectsCopyValues_NoDiagnostic_02()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class DataflowOperationVisitor<TContext>
    where TContext: class
{
    public TContext DataFlowAnalysisContext { get; }
    public DataflowOperationVisitor(TContext t)
    {
        DataFlowAnalysisContext = t;
    }
}

class PointsToDataflowOperationVisitor : DataflowOperationVisitor<A>
{
    public PointsToDataflowOperationVisitor(A context)
        : base(context)
    {
    }
}

class PointsToAnalysis : DataflowAnalysis<A>
{
    public PointsToAnalysis(PointsToDataflowOperationVisitor visitor)
        : base (visitor)
    {
    }

    void M1(A param1)
    {
        using (var a = new A())
        {
            var visitor = new PointsToDataflowOperationVisitor(param1);
            var pointsToAnalysis = new PointsToAnalysis(visitor);
            pointsToAnalysis.GetOrComputeResultCore(param1);
        }
    }
}

class DataflowAnalysis<TContext>
    where TContext: class
{
    private TContext context;
    public DataflowOperationVisitor<TContext> OperationVisitor { get; }
    public DataflowAnalysis(DataflowOperationVisitor<TContext> visitor)
    {
        OperationVisitor = visitor;
    }

    protected void GetOrComputeResultCore(TContext param1)
    {
        if (param1 == null)
        {
            throw new ArgumentNullException(nameof(param1));
        }

        param1 = context;
    }
}
");
        }

        [Theory]
        [InlineData(DisposeAnalysisKind.AllPaths)]
        [InlineData(DisposeAnalysisKind.AllPathsOnlyNotDisposed)]
        [InlineData(DisposeAnalysisKind.NonExceptionPaths)]
        [InlineData(DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed)]
        internal void ExceptionFromCatch_Diagnostic(DisposeAnalysisKind disposeAnalysisKind)
        {
            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (disposeAnalysisKind.AreExceptionPathsAndMayBeNotDisposedViolationsEnabled())
            {
                expectedDiagnostics = new[]
                {
                    // Test0.cs(15,17): warning CA2000: Use recommended dispose pattern to ensure that object created by 'new A()' is disposed on all exception paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.
                    GetCSharpMayBeNotDisposedOnExceptionPathsResultAt(15, 17, "new A()")
                };
            }

            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class C
{
    public void M()
    {
        var a = new A();
        try
        {
            int.Parse(null);
            a.Dispose();
        }
        catch (Exception ex)
        {
            throw new MyException(ex);
        }
    }
}

class MyException: Exception
{
    private const string MyExceptionMessage = nameof(MyExceptionMessage);
    public MyException(Exception inner)
        : base (MyExceptionMessage, inner)
    {
    }
}
", GetEditorConfigFile(disposeAnalysisKind), expectedDiagnostics);
        }

        [Fact]
        public void InvocationOfLambdaCachedOntoField_InterproceduralAnalysis()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    private readonly Func<int> _getInt;
    public A(Func<int> getInt)
    {
        _getInt = getInt;
    }

    private static A Create()
    {
        var a = new A(() => 0);
        return a;
    }

    public static int CreateAndExecute()
    {
        var a = Create();
        return a.Execute();
    }

    private int Execute()
    {
        return _getInt();
    }

    public void Dispose()
    {
    }
}
");
        }

        [Fact]
        public void InvocationOfLocalFunctionCachedOntoField_InterproceduralAnalysis()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    private readonly Func<int> _getInt;
    public A(Func<int> getInt)
    {
        _getInt = getInt;
    }

    private static A Create()
    {
        var a = new A(Create);
        return a;

        int Create() => 0;
    }

    public static int CreateAndExecute()
    {
        var a = Create();
        return a.Execute();
    }

    private int Execute()
    {
        return _getInt();
    }

    public void Dispose()
    {
    }
}
");
        }

        [Fact]
        public void InvocationOfMethodDelegate_PriorInterproceduralCallChain()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    private readonly Func<int> _coreExecute;
    public A()
    {
        _coreExecute = this.CoreExecute;
    }

    private int CoreExecute() => 0;

    private int Execute()
    {
        return _coreExecute();
    }

    public static int CreateAndExecute()
    {
        var a = new A();
        return a.Execute();
    }

    public void Dispose()
    {
    }
}
");
        }

        [Fact]
        public void RecursiveInvocationWithConditionalAccess_InterproceduralAnalysis()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public void M(A a1)
    {
        var a2 = new A();
        a1?.M(a2);
    }

    public void Dispose()
    {
    }
}
",
            // Test0.cs(8,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(8, 18, "new A()"));
        }

        [Fact]
        public void StaticExtensionMethodInvokedAsDelegate_InterproceduralAnalysis()
        {
            VerifyCSharp(@"
using System;

internal delegate bool MyPredicate<in T>(T obj);

internal class A : IDisposable
{
    public void Dispose()
    {
    }
}

internal class B
{
    public void M(int i)
    {
        var a1 = new A();
        M2(a1.ExtensionMethod, i);
    }

    public void M2(MyPredicate<int> predicate, int i)
    {
        if (predicate(i))
        {
        }
    }
}

internal static class AExtensions
{
    public static bool ExtensionMethod(this A a, int i) => i != 0;
}
",
            // Test0.cs(17,18): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A()' before all references to it are out of scope.
            GetCSharpResultAt(17, 18, "new A()"));
        }

        [Fact]
        public void InfiniteAnalysesIterationBug_InterproceduralAnalysis()
        {
            VerifyCSharp(@"
using System;
using System.Collections.ObjectModel;

internal static class Extensions
{
    internal static bool TryGetEvalAttribute<T>(this DkmClrType type, out DkmClrType attributeTarget, out T evalAttribute)
            where T : DkmClrEvalAttribute
    {
        attributeTarget = null;
        evalAttribute = null;

        var underlyingType = type.GetLmrType();
        while ((underlyingType != null) && !underlyingType.IsObject())
        {
            foreach (var attribute in type.GetEvalAttributes())
            {
                evalAttribute = attribute as T;
                if (evalAttribute != null)
                {
                    attributeTarget = type;
                    return true;
                }
            }

            underlyingType = underlyingType.GetBaseTypeOrNull(out type);
        }

        return false;
    }

    internal static bool IsObject(this CustomType type)
    {
        bool result = type.IsClass && (type.BaseType == null) && !type.IsPointer;
        return result;
    }

    internal static CustomType GetBaseTypeOrNull(this CustomType underlyingType, out DkmClrType type)
    {
        underlyingType = underlyingType.BaseType;
        type = (underlyingType != null) ? DkmClrType.Create(underlyingType) : null;

        return underlyingType;
    }
}

internal class DkmClrEvalAttribute : Attribute
{
}

internal class DkmClrType : IDisposable
{
    public CustomType CustomType { get; }
    public ReadOnlyCollection<DkmClrEvalAttribute> EvalAttributes { get; }

    internal static DkmClrType Create(CustomType underlyingType) => new DkmClrType();

    internal CustomType GetLmrType() => CustomType;
    public ReadOnlyCollection<DkmClrEvalAttribute> GetEvalAttributes()
        => EvalAttributes;

    public void Dispose()
    {
    }
}

public class CustomType : IDisposable
{
    public bool IsClass { get; internal set; }
    public CustomType BaseType { get; internal set; }
    public bool IsPointer { get; internal set; }

    public void Dispose()
    {
    }
}
");
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public void ReturnDisposableObjectWrappenInTask_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public Task<C> M1_Task()
    {
        return Task.FromResult(new C());
    }
}
");
            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function M1_Task() As Task(Of C)
        Return Task.FromResult(New C())
    End Function
End Class");
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public void AwaitedButNotDisposed_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public Task<C> M1_Task()
    {
        return Task.FromResult(new C());
    }

    public async Task M2_Task()
    {
        var c = await M1_Task().ConfigureAwait(false);
    }
}
",
            // Test0.cs(18,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'await M1_Task().ConfigureAwait(false)' before all references to it are out of scope.
            GetCSharpResultAt(18, 17, "await M1_Task().ConfigureAwait(false)"));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function M1_Task() As Task(Of C)
        Return Task.FromResult(New C())
    End Function

    Public Async Function M2_Task() As Task
        Dim c = Await M1_Task().ConfigureAwait(False)
    End Function
End Class",
            // Test0.vb(16,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'Await M1_Task().ConfigureAwait(False)' before all references to it are out of scope.
            GetBasicResultAt(16, 17, "Await M1_Task().ConfigureAwait(False)"));
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public void AwaitedButNotDisposed_TaskWrappingField_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    private C _c;
    public void Dispose()
    {
    }

    public Task<C> M1_Task()
    {
        return Task.FromResult(_c);
    }

    public async Task M2_Task()
    {
        var c = await M1_Task().ConfigureAwait(false);
    }
}
",
            // Test0.cs(19,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'await M1_Task().ConfigureAwait(false)' before all references to it are out of scope.
            GetCSharpResultAt(19, 17, "await M1_Task().ConfigureAwait(false)"));

            VerifyBasic(@"
Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Private _c As C

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function M1_Task() As Task(Of C)
        Return Task.FromResult(_c)
    End Function

    Public Async Function M2_Task() As Task
        Dim c = Await M1_Task().ConfigureAwait(False)
    End Function
End Class",
            // Test0.vb(18,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'Await M1_Task().ConfigureAwait(False)' before all references to it are out of scope.
            GetBasicResultAt(18, 17, "Await M1_Task().ConfigureAwait(False)"));
        }

        [Fact, WorkItem(2347, "https://github.com/dotnet/roslyn-analyzers/issues/2347")]
        public void ReturnDisposableObjectInAsyncMethod_DisposedInCaller_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public async Task<C> M1_Task(object context)
    {
        await Task.Yield();
        return new C();
    }

    public async Task M2_Task()
    {
        var c = await M1_Task(null).ConfigureAwait(false);
        c.Dispose();
    }
}");
        }

        [Fact, WorkItem(2347, "https://github.com/dotnet/roslyn-analyzers/issues/2347")]
        public void ReturnDisposableObjectInAsyncMethod_NotDisposedInCaller_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public async Task<C> M1_Task(object context)
    {
        await Task.Yield();
        return new C();
    }

    public async Task M2_Task()
    {
        var c = await M1_Task(null).ConfigureAwait(false);
    }
}",
            // Test0.cs(19,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'await M1_Task(null).ConfigureAwait(false)' before all references to it are out of scope.
            GetCSharpResultAt(19, 17, "await M1_Task(null).ConfigureAwait(false)"));
        }

        [Fact, WorkItem(2361, "https://github.com/dotnet/roslyn-analyzers/issues/2361")]
        public void ExpressionBodiedMethod_ReturnsDisposableObject_NoDiagnostic()
        {
            VerifyCSharp(@"
using System.IO;

class C
{
    Stream M() => File.OpenRead(""C:/somewhere/"");
}");
        }

        [Fact, WorkItem(2361, "https://github.com/dotnet/roslyn-analyzers/issues/2361")]
        public void ReturnsDisposableObject_NotDisposed_Diagnostic()
        {
            VerifyCSharp(@"
using System.IO;

class C
{
    Stream GetStream() => File.OpenRead(""C:/somewhere/"");

    void M2()
    {
        var stream = GetStream();
    }
}",
            // Test0.cs(10,22): warning CA2000: Call System.IDisposable.Dispose on object created by 'GetStream()' before all references to it are out of scope.
            GetCSharpResultAt(10, 22, "GetStream()"));
        }

        [Fact]
        public void PointsToAnalysisAssert_UninitializedLocalPassedToInvocation()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    void M1()
    {
        IDisposable local;
        M2(local);
        local = new C();
    }

    void M2(IDisposable param)
    {
    }

    public void Dispose()
    {
    }
}", TestValidationMode.AllowCompileErrors,
            // Test0.cs(10,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new C()' before all references to it are out of scope.
            GetCSharpResultAt(10, 17, "new C()"));
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public void UsingStatementInCatch()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    public void Dispose() { }

    void M1()
    {
        try
        {
        }
        catch (Exception)
        {
            using (var c = new C())
            {
            }
        }
    }
}");
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public void TryFinallyStatementInCatch()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    public void Dispose() { }

    void M1()
    {
        try
        {
        }
        catch (Exception)
        {
            C c = null;
            try
            {
                c = new C();
            }
            finally
            {
                c.Dispose();
            }
        }
    }
}");
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public void UsingStatementInFinally()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    public void Dispose() { }

    void M1()
    {
        try
        {
        }
        finally
        {
            using (var c = new C())
            {
            }
        }
    }
}");
        }

        [Fact, WorkItem(2506, "https://github.com/dotnet/roslyn-analyzers/issues/2506")]
        public void ErroroneousCodeWithBrokenIfCondition_BailOut_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    public void Dispose() { }

    void M1()
    {
        var c = new C();
        if()
    }
}", TestValidationMode.AllowCompileErrors);
        }

        [Fact, WorkItem(2506, "https://github.com/dotnet/roslyn-analyzers/issues/2506")]
        public void ErroroneousCodeWithBrokenIfCondition_Interprocedural_BailOut_NoDiagnostic()
        {
            VerifyCSharp(@"
using System;

class C : IDisposable
{
    public void Dispose() { }

    void M1()
    {
        var c = new C();
        M2(c);
    }

    void M2(C c)
    {
        if()
    }
}", TestValidationMode.AllowCompileErrors,
            // Test0.cs(10,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new C()' before all references to it are out of scope.
            GetCSharpResultAt(10, 17, "new C()"));
        }

        [Fact, WorkItem(2529, "https://github.com/dotnet/roslyn-analyzers/issues/2529")]
        public void MultilineDisposableCreation_SingleLine_Diagnostic()
        {
            VerifyCSharp(@"
using System;

class A : IDisposable
{
    public A(int a) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = new A(
            M2());
    }

    int M2() => 0;
}",
            // Test0.cs(16,17): warning CA2000: Call System.IDisposable.Dispose on object created by 'new A(' before all references to it are out of scope.
            GetCSharpResultAt(16, 17, "new A("));
        }
    }
}
