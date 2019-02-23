using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessImage.Models
{
    public class BlobFile
    {
        public string Url { get; set; }
        public string Name { get; set; }

        public BlobFile(string url, string name)
        {
            Url = url;
            Name = name;
        }
    }
}
