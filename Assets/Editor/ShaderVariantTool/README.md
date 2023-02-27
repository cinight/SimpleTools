# ShaderVariantTool
This tool lists out all shader keywords and variant counts that are being included in player build and before stripping. \
Expand a shader and see it's keyword table. e.g. If a shader keyword is being completely stripped, the Comiled Count for that keyword should be 0 in the after scriptable stripping column.
\
\
Unity 2022.2.6f1+

### How to use:
1. Add the scripts to an Editor folder
2. Make a player build
3. Top > Windows > ShaderVariantTool
Note: The tool also generates a .csv file under the project root folder. \
It contains more information and it's useful for keeping a record.
\
\
![](README01.jpg)

### FAQ:
1. What is Compiled Count?
   - Each entry on the tool = each keyword in a shader snippet before / after scriptable stripping.
   - The column "Complied Count" = how many variants in that snippet contain this keyword.
2. Shouldn't the tool show all the keywords in every individual variant?
   - In most of my use case, I care more about what keywords are being included (i.e. not stripped) in the build, instead of individual variant, which the keyword list for each variant could be too many/long to look at.
   - If you do need the detailed variant list, use [Project Auditor](https://github.com/Unity-Technologies/ProjectAuditor/blob/master/Documentation~/Installing.md#package-manager-ui-recommended)
