using System;
using System.Collections;

namespace work.bacome.apidocumentation
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal class cAPIDocumentationTemplate
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public cAPIDocumentationTemplate() { }

        /// <summary>
        /// Determines whether this instance and the specified object are the same.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject)
        {
            return base.Equals(pObject);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Determines whether two instances are the same.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public bool Equality(cAPIDocumentationTemplate pA, cAPIDocumentationTemplate pB) { return false; }

        /// <summary>
        /// Determines whether two instances are the different.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public bool Inequality(cAPIDocumentationTemplate pA, cAPIDocumentationTemplate pB) { return false; }

        /// <summary>
        /// Gets one item.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string this[int i] => null;

        /// <summary>
        /// Compares this instance with the specified object.
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public int CompareTo(object pOther) => 0;

        /**<summary>Gets the number of items in the set.</summary>*/
        public int Count => 0;

        /**<summary>Returns an enumerator that iterates through the items in the set.</summary>*/
        public IEnumerator GetEnumerator() => null;
    }

    /// <summary>
    /// Contains ...
    /// </summary>
    internal static class cAPIDocumentationExample1
    {
        /**<summary>fred</summary>*/
        public const string Constant = "fred";
    }

    /// <summary>
    /// The ...
    /// </summary>
    internal enum eAPIDocumentationExample
    {
        /**<summary>The ...</summary>*/
        fred,
        angus,
        charlie
    }

    /// <summary>
    /// Represents a ...
    /// A ... collection.
    /// A ... list.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate" select="returns|remarks"/>
    internal class cAPIDocumentationExample2
    {
        /**<summary>An apidocumentation that represents ...</summary>*/
        public static readonly cAPIDocumentationExample2 PSR = new cAPIDocumentationExample2();

        /// <summary>
        /// The field1.
        /// </summary>
        public readonly bool Field1 = false;

        /// <summary>
        /// Initialises a new instance {so it ... | with the specified ... }
        /// </summary>
        public cAPIDocumentationExample2(bool pParameter) { }

        /// <summary>
        /// Determines whether the { collection | list } contains ...
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public bool Contains(string pName) { return false; }

        /// <summary>
        /// Returns ...
        /// </summary>
        /// <returns></returns>
        public string AMethod() { return string.Empty; }

        /// <summary>
        /// Returns a new instance containing [a copy of] ...
        /// </summary>
        /// <param name="pParam"></param>
        public static implicit operator cAPIDocumentationExample2(string pParam) => null;

        /// <inheritdoc cref="cAPIDocumentationTemplate.cAPIDocumentationTemplate"/>
        public cAPIDocumentationExample2() { }

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(object pOther) => 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator GetEnumerator() => null;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals"/>
        public override bool Equals(object pObject)
        {
            return base.Equals(pObject);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cAPIDocumentationExample2 pA, cAPIDocumentationExample2 pB) { return false; }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cAPIDocumentationExample2 pA, cAPIDocumentationExample2 pB) { return false; }
    }
}