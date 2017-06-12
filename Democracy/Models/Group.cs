﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Democracy.Models
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }
        
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(50, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 3)]
        public string Description { get; set; }
    }
}