using Content.Localization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Resources
{
    public static class psttest
    {

        public static IContentLocalizer Content { get; set; }

        /// <summary>
        /// Call in application shutdown to gracefully handle cancellation
        /// </summary>
        public static void Close()
        {
            if (Content is IDisposable disposable)
                disposable.Dispose();
        }

       ///<summary>
       ///
       ///</summary>
        public static string Banner => Content["Banner"]; 
       ///<summary>
       ///
       ///</summary>
        public static string CheckBox => Content["CheckBox"];
       ///<summary>
       ///
       ///</summary>
        public static string DateField => Content["DateField"];
       ///<summary>
       ///
       ///</summary>
        public static string Decimal => Content["Decimal"];
       ///<summary>
       ///
       ///</summary>
        public static string DropDown => Content["DropDown"];
       ///<summary>
       ///
       ///</summary>
        public static string ImageTest => Content["ImageTest"];
       ///<summary>
       ///
       ///</summary>
        public static string Number => Content["Number"];
       ///<summary>
       ///
       ///</summary>
        public static string TextSample => Content["TextSample"];
       ///<summary>
       ///
       ///</summary>
        public static string Widget => Content["Widget"];


    }
}