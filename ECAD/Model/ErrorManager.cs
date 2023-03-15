using System;
using System.Collections.Generic;

namespace ECAD {
	internal class ErrorManager{
		private readonly Dictionary<string,string> errors=new();
		public ErrorManager(){
			errors["BadHorizontalPos"]="Error loading subchip %1. Malformed horizontal position.";
			errors["BadVerticalPos"]="Error loading subchip %1. Malformed vertical position.";
			errors["BadRotation"]="Error loading subchip %1. Malformed rotation.";
			errors["CipNotFound"]="Cip template error @ link %1. %2 cip not found.";
			errors["PinNotFound"]="Cip template error @ link %1. Pin not found - %2.";
			errors["LinkTriggerNotOutput"]="Cip template error @ link %1. Cannot link from %2 - pin must be pio_Output.";
			errors["LinkTriggerNotInput"]="Cip template error @ link %1. Cannot link from %2 - pin must be pio_Input.";
			errors["LinkTargetNotInput"]="Cip template error @ link %1. Cannot link to %2 - pin must be pio_Input.";
			errors["LinkTargetNotOutput"]="Cip template error @ link %1. Cannot link to %2 - pin must be pio_Output.";
			errors["CannotLoadComponent"]="Error loading subchip %1";
			errors["CannotReadTemplateFile"]="Template file '%1.cip' could not be read. Make sure it exist and is accessible.";
			errors["TemplateFileParsingError"]="Error loading chip! Check definition file.";
			errors["LinkWidthNotConstant"]="Cip template error @ link %1. 'From' amount not the same as 'To' amount (%2).";}
		public void Throw(string name,string param1="",string param2=""){
			string msg=errors[name].Replace("%1",param1).Replace("%2",param2);
			throw new Exception(msg);}
		public void ChainThrow(string name,Exception e,string param1="",string param2=""){
			string msg=errors[name].Replace("%1",param1).Replace("%2",param2);
			throw new Exception(msg,e);}}}
