using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TfsCommitSearcher
{
    public partial class DevOpsChangelog
    {
        public string CollectionName { get; set; }
        public string ProjectName { get; set; }
        public bool Git { get; set; }
        public int ChangesetId { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public DateTime? Date { get; set; }
        public string Comments { get; set; }
    }
}
