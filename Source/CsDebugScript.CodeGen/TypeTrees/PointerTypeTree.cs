﻿namespace CsDebugScript.CodeGen.TypeTrees
{
    /// <summary>
    /// Type tree that represents pointer type.
    /// </summary>
    /// <seealso cref="TypeTree" />
    internal class PointerTypeTree : TypeTree
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointerTypeTree"/> class.
        /// </summary>
        /// <param name="elementType">The element type tree.</param>
        public PointerTypeTree(TypeTree elementType)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// Gets the element type tree.
        /// </summary>
        public TypeTree ElementType { get; private set; }

        /// <summary>
        /// Gets the string representing this type tree in C# code.
        /// </summary>
        /// <returns>
        /// The string representing this type tree in C# code.
        /// </returns>
        public override string GetTypeString()
        {
            return string.Format("CodePointer<{0}>", ElementType.GetTypeString());
        }
    }
}
