using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace MyMediaList.System
{
    public interface IAtom
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Begins editing the object.</summary>
        /// <param name="session">Session.</param>
        public void BeginEdit(Session session);


        /// <summary>Saves the object.</summary>
        public void Save();


        /// <summary>Deletes the object.</summary>
        public void Delete();


        /// <summary>Refreshes the object.</summary>
        public void Refresh();
    }
}
