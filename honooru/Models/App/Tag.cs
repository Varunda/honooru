using System;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Models.App {

    public class Tag {

        /// <summary>
        ///     unique ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     name of this tag. must be unique
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     ID of the <see cref="TagType"/> this tag is for
        /// </summary>
        public ulong TypeID { get; set; }

        /// <summary>
        ///     when this <see cref="Tag"/> was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
