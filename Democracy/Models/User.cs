﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Democracy.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Display(Name = "E-Mail")]
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(100, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 7)]
        [DataType(DataType.EmailAddress)]
        [Index("UserNameIndex", IsUnique =true)]
        public string UserName { get; set; }
        [Display(Name = "First name")]
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(50, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 2)]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(50, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 2)]
        public string LastName { get; set; }

        [Display(Name = "User")]
        //SI LA PROPIEDAD SOLO TIENE EL METODO GET, NO SE AGREGA COMO COLUMNA EN LA BASE DE DATOS
        public string FullName { get { return string.Format("{0} {1}",this.FirstName,this.LastName); } }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(20, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 7)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(100, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 10)]
        public string Address { get; set; }
        public string Grade { get; set; }
        public string Group { get; set; }


        //Esta validacion de StringLenght miniumnLenght:5  impide que se ingresen imagenes vacias(Que no se seleccione imagenes), Devuelve un error en la vista
        [StringLength(200, ErrorMessage = "The field {0} must contain between {2} and {1} characters", MinimumLength = 5)]

        [DataType(DataType.ImageUrl)]
        public string Photo { get; set; }

        
        [JsonIgnore]
        public virtual ICollection<GroupMember> GroupMembers { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<Candidate> Candidates { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<VotingDetail> VotingDetails { get; set; }

    }
}