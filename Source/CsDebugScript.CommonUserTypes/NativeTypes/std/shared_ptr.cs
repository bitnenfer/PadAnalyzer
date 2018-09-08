﻿using CsDebugScript.Exceptions;
using System;
using System.Text;

namespace CsDebugScript.CommonUserTypes.NativeTypes.std
{
    /// <summary>
    /// Implementation of std::shared_ptr
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class shared_ptr<T> : UserType
    {
        /// <summary>
        /// Interface that describes shared_ptr instance abilities.
        /// </summary>
        private interface Ishared_ptr
        {
            /// <summary>
            /// Gets the shared count.
            /// </summary>
            int SharedCount { get; }

            /// <summary>
            /// Gets the weak count.
            /// </summary>
            int WeakCount { get; }

            /// <summary>
            /// Gets the dereferenced pointer.
            /// </summary>
            T Element { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            bool IsEmpty { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is created with make shared.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is created with make shared; otherwise, <c>false</c>.
            /// </value>
            bool IsCreatedWithMakeShared { get; }
        }

        /// <summary>
        /// Microsoft Visual Studio implementations of std::shared_ptr
        /// </summary>
        public class VisualStudio : Ishared_ptr
        {
            /// <summary>
            /// The pointer field
            /// </summary>
            private UserMember<Variable> pointer;

            /// <summary>
            /// The dereferenced pointer
            /// </summary>
            private UserMember<T> element;

            /// <summary>
            /// The shared count
            /// </summary>
            private UserMember<int> sharedCount;

            /// <summary>
            /// The weak count
            /// </summary>
            private UserMember<int> weakCount;

            /// <summary>
            /// Flag that indicated whether this instance was created with make shared
            /// </summary>
            private UserMember<bool> isCreatedWithMakeShared;

            /// <summary>
            /// Initializes a new instance of the <see cref="VisualStudio"/> class.
            /// </summary>
            /// <param name="variable">The variable.</param>
            public VisualStudio(Variable variable)
            {
                // Initialize members
                pointer = UserMember.Create(() => variable.GetField("_Ptr"));
                element = UserMember.Create(() => pointer.Value.DereferencePointer().CastAs<T>());
                sharedCount = UserMember.Create(() => (int)variable.GetField("_Rep").GetField("_Uses"));
                weakCount = UserMember.Create(() => (int)variable.GetField("_Rep").GetField("_Weaks"));
                isCreatedWithMakeShared = UserMember.Create(() => variable.GetField("_Rep").DowncastInterface().GetCodeType().Name.StartsWith("std::_Ref_count_obj<"));
            }

            /// <summary>
            /// Gets a value indicating whether this instance is created with make shared.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is created with make shared; otherwise, <c>false</c>.
            /// </value>
            public bool IsCreatedWithMakeShared
            {
                get
                {
                    return isCreatedWithMakeShared.Value;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    return pointer.Value.IsNull();
                }
            }

            /// <summary>
            /// Gets the dereferenced pointer.
            /// </summary>
            public T Element
            {
                get
                {
                    return element.Value;
                }
            }

            /// <summary>
            /// Gets the shared count.
            /// </summary>
            public int SharedCount
            {
                get
                {
                    return sharedCount.Value;
                }
            }

            /// <summary>
            /// Gets the weak count.
            /// </summary>
            public int WeakCount
            {
                get
                {
                    return weakCount.Value;
                }
            }

            /// <summary>
            /// Verifies if the specified code type is correct for this class.
            /// </summary>
            /// <param name="codeType">The code type.</param>
            internal static bool VerifyCodeType(CodeType codeType)
            {
                // We want to have this kind of hierarchy
                // _Ptr
                // _Rep
                // | _Uses
                // | _Weaks
                CodeType _Rep, _Ptr, _Uses, _Weaks;

                if (!codeType.GetFieldTypes().TryGetValue("_Ptr", out _Ptr))
                    return false;
                if (!codeType.GetFieldTypes().TryGetValue("_Rep", out _Rep))
                    return false;
                if (!_Rep.GetFieldTypes().TryGetValue("_Uses", out _Uses))
                    return false;
                if (!_Rep.GetFieldTypes().TryGetValue("_Weaks", out _Weaks))
                    return false;
                return true;
            }
        }

        /// <summary>
        /// libstdc++ 6 implementations of std::shared_ptr
        /// </summary>
        public class LibStdCpp6 : Ishared_ptr
        {
            /// <summary>
            /// The pointer field
            /// </summary>
            private UserMember<Variable> pointer;

            /// <summary>
            /// The dereferenced pointer
            /// </summary>
            private UserMember<T> element;

            /// <summary>
            /// The shared count
            /// </summary>
            private UserMember<int> sharedCount;

            /// <summary>
            /// The weak count
            /// </summary>
            private UserMember<int> weakCount;

            /// <summary>
            /// Flag that indicated whether this instance was created with make shared
            /// </summary>
            private UserMember<bool> isCreatedWithMakeShared;

            /// <summary>
            /// Initializes a new instance of the <see cref="LibStdCpp6"/> class.
            /// </summary>
            /// <param name="variable">The variable.</param>
            public LibStdCpp6(Variable variable)
            {
                // Initialize members
                pointer = UserMember.Create(() => variable.GetField("_M_ptr"));
                element = UserMember.Create(() => pointer.Value.DereferencePointer().CastAs<T>());
                sharedCount = UserMember.Create(() => (int)variable.GetField("_M_refcount").GetField("_M_pi").GetField("_M_use_count"));
                weakCount = UserMember.Create(() => (int)variable.GetField("_M_refcount").GetField("_M_pi").GetField("_M_weak_count"));
                isCreatedWithMakeShared = UserMember.Create(() =>
                {
                    CodeType codeType = variable.GetField("_M_refcount").GetField("_M_pi").DowncastInterface().GetCodeType();

                    if (codeType.Name.StartsWith("std::_Sp_counted_ptr_inplace<"))
                    {
                        return true;
                    }

                    if (!codeType.Name.StartsWith("std::_Sp_counted_deleter<"))
                    {
                        return false;
                    }

                    try
                    {
                        codeType = (CodeType)codeType.TemplateArguments[1];
                        return codeType.Name.StartsWith("std::__shared_ptr<") && codeType.Name.Contains("::_Deleter<");
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            /// <summary>
            /// Gets a value indicating whether this instance is created with make shared.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is created with make shared; otherwise, <c>false</c>.
            /// </value>
            public bool IsCreatedWithMakeShared
            {
                get
                {
                    return isCreatedWithMakeShared.Value;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    return pointer.Value.IsNull();
                }
            }

            /// <summary>
            /// Gets the dereferenced pointer.
            /// </summary>
            public T Element
            {
                get
                {
                    return element.Value;
                }
            }

            /// <summary>
            /// Gets the shared count.
            /// </summary>
            public int SharedCount
            {
                get
                {
                    return sharedCount.Value;
                }
            }

            /// <summary>
            /// Gets the weak count.
            /// </summary>
            public int WeakCount
            {
                get
                {
                    return weakCount.Value;
                }
            }

            /// <summary>
            /// Verifies if the specified code type is correct for this class.
            /// </summary>
            /// <param name="codeType">The code type.</param>
            internal static bool VerifyCodeType(CodeType codeType)
            {
                // We want to have this kind of hierarchy
                // _M_ptr
                // _M_refcount
                // | _M_pi
                //   | _M_use_count
                //   | _M_weak_count
                CodeType _M_refcount, _M_pi, _M_ptr, _M_use_count, _M_weak_count;

                if (!codeType.GetFieldTypes().TryGetValue("_M_ptr", out _M_ptr))
                    return false;
                if (!codeType.GetFieldTypes().TryGetValue("_M_refcount", out _M_refcount))
                    return false;
                if (!_M_refcount.GetFieldTypes().TryGetValue("_M_pi", out _M_pi))
                    return false;
                if (!_M_pi.GetFieldTypes().TryGetValue("_M_use_count", out _M_use_count))
                    return false;
                if (!_M_pi.GetFieldTypes().TryGetValue("_M_weak_count", out _M_weak_count))
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Clang libc++ implementation of std::shared_ptr
        /// </summary>
        public class ClangLibCpp : Ishared_ptr
        {
            /// <summary>
            /// The pointer field
            /// </summary>
            private UserMember<Variable> pointer;

            /// <summary>
            /// The dereferenced pointer
            /// </summary>
            private UserMember<T> element;

            /// <summary>
            /// The shared count
            /// </summary>
            private UserMember<int> sharedCount;

            /// <summary>
            /// The weak count
            /// </summary>
            private UserMember<int> weakCount;

            /// <summary>
            /// Flag that indicated whether this instance was created with make shared
            /// </summary>
            private UserMember<bool> isCreatedWithMakeShared;

            /// <summary>
            /// Initializes a new instance of the <see cref="ClangLibCpp"/> class.
            /// </summary>
            /// <param name="variable">The variable.</param>
            public ClangLibCpp(Variable variable)
            {
                // Initialize members
                pointer = UserMember.Create(() => variable.GetField("__ptr_"));
                element = UserMember.Create(() => pointer.Value.DereferencePointer().CastAs<T>());
                sharedCount = UserMember.Create(() => (int)variable.GetField("__cntrl_").GetField("__shared_owners_") + 1);
                weakCount = UserMember.Create(() => (int)variable.GetField("__cntrl_").GetField("__shared_weak_owners_") + 1);
                isCreatedWithMakeShared = UserMember.Create(() => variable.GetField("__cntrl_").DowncastInterface().GetCodeType().Name.StartsWith("std::__1::__shared_ptr_emplace<"));
            }

            /// <summary>
            /// Gets a value indicating whether this instance is created with make shared.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is created with make shared; otherwise, <c>false</c>.
            /// </value>
            public bool IsCreatedWithMakeShared
            {
                get
                {
                    return isCreatedWithMakeShared.Value;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    return pointer.Value.IsNull();
                }
            }

            /// <summary>
            /// Gets the dereferenced pointer.
            /// </summary>
            public T Element
            {
                get
                {
                    return element.Value;
                }
            }

            /// <summary>
            /// Gets the shared count.
            /// </summary>
            public int SharedCount
            {
                get
                {
                    return sharedCount.Value;
                }
            }

            /// <summary>
            /// Gets the weak count.
            /// </summary>
            public int WeakCount
            {
                get
                {
                    return weakCount.Value;
                }
            }

            /// <summary>
            /// Verifies if the specified code type is correct for this class.
            /// </summary>
            /// <param name="codeType">The code type.</param>
            internal static bool VerifyCodeType(CodeType codeType)
            {
                // We want to have this kind of hierarchy
                // __ptr_
                // __cntrl_
                // | __shared_owners_
                // | __shared_weak_owners_
                CodeType __ptr_, __cntrl_, __shared_owners_, __shared_weak_owners_;

                if (!codeType.GetFieldTypes().TryGetValue("__ptr_", out __ptr_))
                    return false;
                if (!codeType.GetFieldTypes().TryGetValue("__cntrl_", out __cntrl_))
                    return false;
                if (!__cntrl_.GetFieldTypes().TryGetValue("__shared_owners_", out __shared_owners_))
                    return false;
                if (!__cntrl_.GetFieldTypes().TryGetValue("__shared_weak_owners_", out __shared_weak_owners_))
                    return false;
                return true;
            }
        }

        /// <summary>
        /// The type selector
        /// </summary>
        private static TypeSelector<Ishared_ptr> typeSelector = new TypeSelector<Ishared_ptr>(new[]
        {
            new Tuple<Type, Func<CodeType, bool>>(typeof(VisualStudio), VisualStudio.VerifyCodeType),
            new Tuple<Type, Func<CodeType, bool>>(typeof(LibStdCpp6), LibStdCpp6.VerifyCodeType),
            new Tuple<Type, Func<CodeType, bool>>(typeof(ClangLibCpp), ClangLibCpp.VerifyCodeType),
        });

        /// <summary>
        /// Verifies that type user type can work with the specified code type.
        /// </summary>
        /// <param name="codeType">The code type.</param>
        /// <returns><c>true</c> if user type can work with the specified code type; <c>false</c> otherwise</returns>
        public static bool VerifyCodeType(CodeType codeType)
        {
            return typeSelector.VerifyCodeType(codeType);
        }

        /// <summary>
        /// The instance used to read variable data
        /// </summary>
        private Ishared_ptr instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="shared_ptr{T}"/> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        public shared_ptr(Variable variable)
            : base(variable)
        {
            // Verify code type
            instance = typeSelector.SelectType(variable);
            if (instance == null)
            {
                throw new WrongCodeTypeException(variable, nameof(variable), "std::shared_ptr");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is created with make shared.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is created with make shared; otherwise, <c>false</c>.
        /// </value>
        public bool IsCreatedWithMakeShared
        {
            get
            {
                return instance.IsCreatedWithMakeShared;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get
            {
                return instance.IsEmpty;
            }
        }

        /// <summary>
        /// Gets the dereferenced pointer.
        /// </summary>
        [ForceDefaultVisualizerAtttribute]
        public T Element
        {
            get
            {
                return instance.Element;
            }
        }

        /// <summary>
        /// Gets the shared count.
        /// </summary>
        public int SharedCount
        {
            get
            {
                return instance.SharedCount;
            }
        }

        /// <summary>
        /// Gets the weak count.
        /// </summary>
        public int WeakCount
        {
            get
            {
                return instance.WeakCount;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (IsEmpty)
                return "empty";

            StringBuilder sb = new StringBuilder();

            sb.Append("shared_ptr ");
            sb.Append(Element?.ToString());
            sb.Append(" [");
            sb.Append(SharedCount);
            if (SharedCount == 1)
                sb.Append(" strong ref");
            else
                sb.Append(" strong refs");
            if (WeakCount > 1)
            {
                sb.Append(", ");
                sb.Append(WeakCount - 1);
                if (WeakCount == 2)
                    sb.Append(" weak ref");
                else
                    sb.Append(" weak refs");
            }
            sb.Append("]");
            if (IsCreatedWithMakeShared)
                sb.Append(" [make_shared]");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Simplification class for creating <see cref="shared_ptr{T}"/> with T being <see cref="Variable"/>.
    /// </summary>
    [UserType(TypeName = "std::shared_ptr<>", CodeTypeVerification = nameof(shared_ptr.VerifyCodeType))]
    public class shared_ptr : shared_ptr<Variable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="shared_ptr"/> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        public shared_ptr(Variable variable)
            : base(variable)
        {
        }
    }
}
