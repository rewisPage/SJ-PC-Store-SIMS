using System;

namespace SJ_PC_Store_SIMS.Models
{
    public class AttachmentModel
    {
        public int AttachmentID { get; set; }
        public string PO_Number { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}