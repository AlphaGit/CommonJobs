﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using CommonJobs.Utilities;
using NLog;

namespace CommonJobs.Domain
{
    public class Employee: Person
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public Employee()
        {
            InitializeLists();
        }

        public Employee(string name): base(name)
        {
            InitializeLists();
        }

        private void InitializeLists()
        {
            Absences = new List<Absence>();
            Vacations = new List<Vacation>();
            AttachmentsBySlot = new List<SlotWithAttachment>();
        }

        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Display(Name = "E-mail corporativo")]
        [DataType(DataType.EmailAddress)]
        public string CorporativeEmail { get; set; }

        [Display(Name = "Fecha Inicio")]
        [DataType(DataType.Date)]
        public DateTime? HiringDate { get; set; }

        [Display(Name = "Rem. Inicial")]
        [DataType(DataType.Currency)]
        public Decimal InitialSalary 
        {
            get
            {
                return SalaryChanges
                    .EmptyIfNull()
                    .OrderBy(x => x.RealDate)
                    .Select(x => (decimal?)x.Salary)
                    .FirstOrDefault() ?? 0;
            } 
        }

        [Display(Name = "Rem.Actual")]
        [DataType(DataType.Currency)]
        public Decimal CurrentSalary 
        {
            get
            {
                return SalaryChanges
                    .EmptyIfNull()
                    .OrderByDescending(x => x.RealDate)
                    .Select(x => (decimal?)x.Salary)
                    .FirstOrDefault() ?? 0;
            } 
        }

        [Display(Name = "Horario")]
        public string Schedule { get; set; }

        //TODO this should be auto-calculated from Schedule
        [Display(Name = "Carga horaria")]
        //Only in detailed view
        public decimal? WorkingHours { get; set; }
        
        [Display(Name = "Legajo")]
        public string FileId { get; set; }
        
        [Display(Name = "Almuerzo")]
        public bool Lunch { get; set; }
        
        [Display(Name = "Número de Cuenta")]
        public string BankAccount { get; set; }

        [Display(Name = "Sucursal")]
        public string BankBranch { get; set; }

        [Display(Name = "CBU")]
        public string UniqueBankCode { get; set; }

        [Display(Name = "Banco")]
        public string BankName{ get; set; }

        [Display(Name = "CUIL")]
        public string Cuil { get; set; }
        
        [Display(Name = "Obra Social")]
        public string HealthInsurance { get; set; }

        [Display(Name = "Plataforma")]
        public string Platform { get; set; }

        [Display(Name = "Certificaciones")]
        //Only in detailed view
        public List<Certification> Certifications { get; set; }

        [Display(Name = "Posición Inicial")]
        //Only in detailed view
        public string InitialPosition { get; set; }

        [Display(Name = "Posición Actual")]
        public string CurrentPosition { get; set; }

        [Display(Name = "Seniority")]
        //Only in detailed view
        public string Seniority { get; set; }

        [Display(Name = "Proyecto")]
        public string CurrentProject { get; set; }

        [Display(Name = "Convenio")]
        public string Agreement { get; set; }

        [Display(Name = "Ausencias")]
        public List<Absence> Absences { get; set; }

        [Display(Name = "Vacaciones")]
        public List<Vacation> Vacations { get; set; }

        [Display(Name = "Observaciones")]
        public List<NoteWithAttachment> Notes { get; set; }

        [Display(Name = "Historial de sueldos")]
        public List<SalaryChange> SalaryChanges { get; set; }

        [Display(Name = "Posee pasaporte argentino")]
        public bool HasArgentinianPassport { get; set; }

        [Display(Name = "Vencimiento del pasaporte argentino")]
        [DataType(DataType.Date)]
        public DateTime? ArgentinianPassportExpiration { get; set; }

        [Display(Name = "Posee visa de EEUU")]
        public bool HasUsaVisa { get; set; }

        [Display(Name = "Vencimiento de la visa de EEUU")]
        [DataType(DataType.Date)]
        public DateTime? UsaVisaExpiration { get; set; }

        [Display(Name = "Posee ciudadanía europea")]
        public bool HasEuropeanCitizenship { get; set; }

        [Display(Name = "Posee pasaporte europeo")]
        public bool HasEuropeanPassport { get; set; }

        [Display(Name = "Vencimiento del pasaporte europeo")]
        [DataType(DataType.Date)]
        public DateTime? EuropeanPassportExpiration { get; set; }
        
        public override IEnumerable<SlotWithAttachment> AllAttachmentReferences
        {
            get 
            { 
                return base.AllAttachmentReferences
                    .Union(SlotWithAttachment.GenerateFromNotes(Notes))
                    .Union(AttachmentsBySlot.EmptyIfNull().Where(x => x.Attachment != null)); 
            }
        }

        public List<SlotWithAttachment> AttachmentsBySlot { get; set; }

        public SlotWithAttachment AddAttachment(AttachmentReference attachmentReference, AttachmentSlot slot)
        {
            if (AttachmentsBySlot == null)
            {
                AttachmentsBySlot = new List<SlotWithAttachment>();
            }
            else if (AttachmentsBySlot.Any(x => x.SlotId == slot.Id && x.Attachment != null))
            {
                log.Dump(
                    LogLevel.Error,
                    new { slot, attachmentReference },
                    "Slot is not free");
                throw new ApplicationException(string.Format("Slot `{0}` is not free", slot.Id));
            }
            else
            {
                AttachmentsBySlot.RemoveAll(x => x.Attachment == null);
            }

            var added = new SlotWithAttachment()
            {
                Date = DateTime.Now,
                Attachment = attachmentReference,
                SlotId = slot.Id
            };

            AttachmentsBySlot.Add(added);

            return added;
        }

        public string ApplicantId { get; set; }
    }
}