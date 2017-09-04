using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Model
{

    /// <summary>
    /// Provides attributes common to all objects in the model as well as helper functions.
    /// </summary>
    /// 
    public abstract class BaseEntity
    {

        public BaseEntity() {
            this.UpdateModifyInfo();
        }
        
        public static System.Byte TRUE_FLAG_VALUE = 1;
        public static System.Byte FALSE_FLAG_VALUE = 0;

        public void UpdateModifyInfo()        {          
            this.ModifyTimestamp = DateTime.Now;
        }
        
        [Required]
        public DateTime ModifyTimestamp { get; set; }

    }
}
