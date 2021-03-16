# ShaderVariantTool
This tool lists out all shader variants that are being included in player build. \
i.e. If a shader keyword is being stripped, you won't see the keyword on the list.
\
\
2020.2+ \

### How to use:
1. Add the scripts to an Editor folder: \
ShaderVariantTool.cs \
ShaderVariantTool_ComputePreprocess.cs \
ShaderVariantTool_ShaderPreprocess.cs
2. Make a player build
3. Top > Windows > ShaderVariantTool
\
\
![](README01.jpg)


# ShaderStripping Example scripts

### How to use:
1. Add the scripts to an Editor folder: \
StrippingExample_Shader.cs \
StrippingExample_ComputeShader.cs
2. Edit the scripts so that you can define what you want to strip
3. Make a player build
4. You can use ShaderVariantTool to verify if those variants are stripped

