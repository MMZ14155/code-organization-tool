using System.IO;
using System.Text.RegularExpressions;

namespace code_organization_tool;

public class CodeProcessor
{
    // 处理单个文件内容
    public string ProcessFile(string filePath, bool addFileNameComment, bool removeComments, bool removeImports, bool removeExtraNewlines)
    {
        string content = File.ReadAllText(filePath);
            
        // 如果需要移除注释，先移除
        if (removeComments)
        {
            content = RemoveComments(content, GetFileExtension(filePath));
        }

        // 如果需要移除导入语句
        if (removeImports)
        {
            content = RemoveImports(content, GetFileExtension(filePath));
        }
        
        // 如果需要移除多余空行
        if (removeExtraNewlines)
        {
            content = RemoveExtraNewlines(content);
        }

        // 如果需要添加文件名注释，最后添加
        if (addFileNameComment)
        {
            string fileName = Path.GetFileName(filePath);
            content = AddFileNameComment(fileName, content, GetFileExtension(filePath));
        }

        return content;
    }

    // 添加文件名注释，根据文件类型确定注释风格
    private string AddFileNameComment(string fileName, string content, string fileExtension)
    {
        string commentPrefix = GetCommentPrefix(fileExtension);
        string commentSuffix = GetCommentSuffix(fileExtension);
            
        if (string.IsNullOrEmpty(commentPrefix))
        {
            // 默认使用 C 风格注释
            commentPrefix = "//";
        }
        
        if (string.IsNullOrEmpty(commentSuffix))
        {
            // 默认使用 C 风格注释
            commentSuffix = "";
        }
            
        // 添加空行分隔符
        content = "\n\n" + content;

        return $"{Environment.NewLine}{commentPrefix} {fileName}{commentSuffix}{Environment.NewLine}{content}";
    }

    // 根据文件扩展名获取注释前缀
    private string GetCommentPrefix(string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            case ".c":
            case ".cpp":
            case ".h":
            case ".cs":
            case ".vb":
            case ".java":
            case ".js":
                return "//"; // C 风格注释
            case ".py":
                return "#"; // Python 风格注释
            case ".html":
            case ".xaml":
                return "<!--"; // XAML 风格注释前缀
            default:
                return "//"; // 默认使用 C 风格注释
        }
    }

    // 根据文件扩展名获取注释后缀
    private string GetCommentSuffix(string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            case ".html":
            case ".xaml":
                return "-->"; // XAML 风格注释后缀
            default:
                return ""; // 默认无后缀
        }
    }

    // 移除注释的方法
    private string RemoveComments(string content, string fileExtension)
    {
        // 根据文件类型确定注释格式
        switch (fileExtension.ToLower())
        {
            case ".c":
            case ".cpp":
            case ".h":
            case ".cs":
            case ".vb":
            case ".java":
            case ".js":
                // C / C++ / C# / Java 风格注释
                content = RemoveCStyleComments(content);
                break;
            case ".py":
                // Python 风格注释
                content = RemovePythonComments(content);
                break;
            case ".html":
            case ".xaml":
                // XAML 风格注释
                content = RemoveXamlComments(content);
                break;
            default:
                // 默认处理 C 风格注释
                content = RemoveCStyleComments(content);
                break;
        }

        return content;
    }

    // 移除 C 风格注释
    private string RemoveCStyleComments(string content)
    {
        // 移除单行注释 (//)
        content = Regex.Replace(content, @"(//.*?(\n|\r\n|$))", "");

        // 移除多行注释 (/* */)
        content = Regex.Replace(content, @"(/\*[\s\S]*?\*/)", "");

        return content;
    }

    // 移除 Python 风格注释
    private string RemovePythonComments(string content)
    {
        // 移除单行注释 (#)
        content = Regex.Replace(content, @"(#.*?(\n|\r\n|$))", "");

        // 移除多行注释 (''' 或 """)
        content = Regex.Replace(content, @"('''.*?'''|"""".*?"""")", "");

        return content;
    }
    
    // 移除 XAML 风格注释
    private string RemoveXamlComments(string content)
    {
        // 移除 XAML 注释 (<!-- -->)
        content = Regex.Replace(content, @"(<!--.*?-->)", "", RegexOptions.Singleline);

        return content;
    }
    
    // 移除导入语句
    private string RemoveImports(string content, string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            case ".c":
            case ".cpp":
            case ".h":
                // 移除 C/C++ 的 #include 语句
                content = Regex.Replace(content, @"^\s*#include\s+[<""][^>""]*[>""]\s*$", "", RegexOptions.Multiline);
                break;

            case ".cs":
                // 只移除 using 指令，保留 using 语句块
                content = Regex.Replace(content, @"^\s*using\s+[\w\.]+(\s*=\s*[\w\.<>]+)?\s*;\s*$", "", RegexOptions.Multiline);
                break;

            case ".vb":
                // 移除 VB 的 Imports 语句
                content = Regex.Replace(content, @"^\s*Imports\s+[\w\.]+\s*$", "", RegexOptions.Multiline);
                break;

            case ".java":
                // 移除 Java 的 import 语句
                content = Regex.Replace(content, @"^\s*import\s+[\w\.]+\s*;\s*$", "", RegexOptions.Multiline);
                break;

            case ".py":
                // 移除 Python 的单行 import 和 from ... import ...
                content = Regex.Replace(content, @"^\s*(import\s+[\w\.]+(\s+as\s+\w+)?|from\s+[\w\.]+\s+import\s+[\w\*,\s]+)\s*$", "", RegexOptions.Multiline);
                break;
        }

        // 清理多余空行，保留最多两个换行
        content = Regex.Replace(content, @"(\r?\n){2,}", "\n\n");
        return content.Trim();
    }
    
    // 移除多余空行，保留最多一个空行
    private string RemoveExtraNewlines(string content)
    {
        content = Regex.Replace(content, @"(\r?\n\s*\r?\n)+", "\n\n");
        return content.Trim();
    }

    // 获取文件扩展名
    private string GetFileExtension(string filePath)
    {
        return Path.GetExtension(filePath);
    }
}