namespace code_organization_tool;

public class CodeLineCounter
{
    private readonly CodeProcessor _codeProcessor = new CodeProcessor();

    // 计算给定文件列表在指定处理选项下的总行数
    public int CalculateTotalLines(List<string> filePaths, bool addFileNameComment, bool removeComments, bool removeImports, bool removeExtraNewlines)
    {
        int totalLines = 0;
        foreach (var filePath in filePaths)
        {
            // 处理文件内容
            string content = _codeProcessor.ProcessFile(
                filePath,
                addFileNameComment,
                removeComments,
                removeImports,
                removeExtraNewlines
            );

            // 统计行数（按换行符计算）
            totalLines += content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        return totalLines;
    }
}