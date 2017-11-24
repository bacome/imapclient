using System;
using System.Collections;

namespace work.bacome.imapclient.apidocumentation
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal class cAPIDocumentationTemplate
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is not <see langword="null"/>, events and callbacks are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler or callback then the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </remarks>
        private event Action Event;



        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public cAPIDocumentationTemplate() { }


        /// <summary>
        /// Determines whether two instances are the same.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public bool Equality(cAPIDocumentationTemplate pA, cAPIDocumentationTemplate pB) { return false; }

        /// <summary>
        /// Determines whether two instances are different.
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
        public string Indexer(int i) => null;

        /// <summary>
        /// Compares this instance with the specified object.
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public int CompareTo(object pOther) => 0;

        /**<summary>Gets the number of items in the set.</summary>*/
        public int Count => 0;

        /// <summary>
        /// Returns an enumerator that iterates through the items in the set.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() => null;

        /// <summary>
        /// Returns the hash code for the instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject)
        {
            return base.Equals(pObject);
        }
       

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
        /// Returns | Indicates whether ...
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







        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event Action Event;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(object pOther) => 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator GetEnumerator() => null;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public string this[int i] => null;





        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cAPIDocumentationExample2 pA, cAPIDocumentationExample2 pB) { return false; }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cAPIDocumentationExample2 pA, cAPIDocumentationExample2 pB) { return false; }


        // for when equals isn't an override

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cAPIDocumentationExample2 pObject)
        {
            return false;
        }






        // just use the ones defined by object ....

        /// <inheritdoc />
        public override bool Equals(object pObject)
        {
            return base.Equals(pObject);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString();
        }
    }
}