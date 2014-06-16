﻿/*
* Copyright (c) 2014, Furore (info@furore.com) and contributors
* See the file CONTRIBUTORS for details.
*
* This file is licensed under the BSD 3-Clause license
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Fhir.Profiling
{

    public class ProfileValidator
    {
        private Report report = new Report();
        private Profile profile;
        private List<string> structureNames = new List<string>();
        private ConstraintValidator constraintValidator;

        public ProfileValidator(Profile profile)
        {
            report.Clear();
            this.profile = profile;
            this.constraintValidator = new ConstraintValidator(report);
        }

        public void ValidateConstraint(Element element, Constraint constraint)
        {
            string message;
            bool valid = this.constraintValidator.Validate(constraint, out message);
            if (!valid)
            {
                report.Add("Profile Constraint", Kind.Invalid, null, message);
            }
            
        }

        public void ValidateConstraints(Element element)
        {
            foreach(Constraint c in element.Constraints)
            {
                ValidateConstraint(element, c);
            }
        }

        public void ValidateTypeRef(Element element, TypeRef typeref)
        {
            if (typeref.Structure != null)
            {
                // Dit hoeft niet recursief, want de structures worden op hoofdniveau al gevalideerd
                //ValidateStructure(typeref.Structure);

            }
            else if (typeref.Code == null)
            {
                report.Add("Profile", Kind.Invalid, null, "Missing a reference to a structure in element [{0}]", element.Name);
            }
            else if (Regex.IsMatch(typeref.Code, "[A-Za-z][A-Za-z0-9]*"))
            {
                structureNames.Add(typeref.Code);
            }
            else
            {
                report.Add("Profile", Kind.Invalid, null, string.Format("Invalid structure reference '{0}' in {1}", typeref.Code, string.Join(".", element.Path)));
            }
        }

        public void ValidateTypeRefs(Element element)
        {
            foreach (TypeRef t in element.TypeRefs)
            {
                ValidateTypeRef(element, t);
            }
        }

        public void ValidateElement(Element element)
        {
            ValidateConstraints(element);
            ValidateTypeRefs(element);
        }

        public void ValidateStructure(Structure structure)
        {
            if (structure.IsPrimitive)
                return; // there is no more detail.

            foreach (Element element in structure.Elements)
            {
                ValidateElement(element);
            }
        }

        private void ValidateStructures()
        {
            foreach (Structure structure in profile.Structures)
            {
                ValidateStructure(structure);
            }
        }

        private void AddStructureNamesToReport()
        {
            foreach (string s in structureNames.Distinct())
                report.Add("Profile", Kind.Incomplete, null, "Missing structure definition [{0}]", s);
            
        }

        public Report Validate()
        {
            report.Clear();
            ValidateStructures();
            
            AddStructureNamesToReport();

            return report;
        }
    }
}