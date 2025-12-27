using System.Text;

namespace FieldTicket.Infrastructure.Utils;

/// <summary>
/// 拼音转换工具类
/// 将中文姓名转换为拼音（用于生成密码）
/// </summary>
public static class PinyinHelper
{
    /// <summary>
    /// 常用姓氏拼音映射表
    /// </summary>
    private static readonly Dictionary<string, string> SurnameMap = new()
    {
        // 百家姓常用姓氏
        {"王", "wang"}, {"李", "li"}, {"张", "zhang"}, {"刘", "liu"}, {"陈", "chen"},
        {"杨", "yang"}, {"赵", "zhao"}, {"黄", "huang"}, {"周", "zhou"}, {"吴", "wu"},
        {"徐", "xu"}, {"孙", "sun"}, {"胡", "hu"}, {"朱", "zhu"}, {"高", "gao"},
        {"林", "lin"}, {"何", "he"}, {"郭", "guo"}, {"马", "ma"}, {"罗", "luo"},
        {"梁", "liang"}, {"宋", "song"}, {"郑", "zheng"}, {"谢", "xie"}, {"韩", "han"},
        {"唐", "tang"}, {"冯", "feng"}, {"于", "yu"}, {"董", "dong"}, {"萧", "xiao"},
        {"程", "cheng"}, {"曹", "cao"}, {"袁", "yuan"}, {"邓", "deng"}, {"许", "xu"},
        {"傅", "fu"}, {"沈", "shen"}, {"曾", "zeng"}, {"彭", "peng"}, {"吕", "lv"},
        {"苏", "su"}, {"卢", "lu"}, {"蒋", "jiang"}, {"蔡", "cai"}, {"贾", "jia"},
        {"丁", "ding"}, {"魏", "wei"}, {"薛", "xue"}, {"叶", "ye"}, {"阎", "yan"},
        {"余", "yu"}, {"潘", "pan"}, {"杜", "du"}, {"戴", "dai"}, {"夏", "xia"},
        {"钟", "zhong"}, {"汪", "wang"}, {"田", "tian"}, {"任", "ren"}, {"姜", "jiang"},
        {"范", "fan"}, {"方", "fang"}, {"石", "shi"}, {"姚", "yao"}, {"谭", "tan"},
        {"廖", "liao"}, {"邹", "zou"}, {"熊", "xiong"}, {"金", "jin"}, {"陆", "lu"},
        {"郝", "hao"}, {"孔", "kong"}, {"白", "bai"}, {"崔", "cui"}, {"康", "kang"},
        {"毛", "mao"}, {"邱", "qiu"}, {"秦", "qin"}, {"江", "jiang"}, {"史", "shi"},
        {"顾", "gu"}, {"侯", "hou"}, {"邵", "shao"}, {"孟", "meng"}, {"龙", "long"},
        {"万", "wan"}, {"段", "duan"}, {"漕", "cao"}, {"钱", "qian"}, {"汤", "tang"},
    };

    /// <summary>
    /// 常用字拼音映射表（可扩展）
    /// </summary>
    private static readonly Dictionary<string, string> CharMap = new()
    {
        // 数字
        {"一", "yi"}, {"二", "er"}, {"三", "san"}, {"四", "si"}, {"五", "wu"},
        {"六", "liu"}, {"七", "qi"}, {"八", "ba"}, {"九", "jiu"}, {"十", "shi"},

        // 常用字
        {"明", "ming"}, {"华", "hua"}, {"强", "qiang"}, {"军", "jun"}, {"伟", "wei"},
        {"敏", "min"}, {"杰", "jie"}, {"娜", "na"}, {"静", "jing"}, {"丽", "li"},
        {"芳", "fang"}, {"秀", "xiu"}, {"国", "guo"}, {"文", "wen"}, {"平", "ping"},
        {"建", "jian"}, {"云", "yun"}, {"海", "hai"}, {"江", "jiang"}, {"波", "bo"},
        {"涛", "tao"}, {"鹏", "peng"}, {"飞", "fei"}, {"龙", "long"}, {"凤", "feng"},
        {"艳", "yan"}, {"红", "hong"}, {"霞", "xia"}, {"莉", "li"}, {"玲", "ling"},
        {"宇", "yu"}, {"浩", "hao"}, {"阳", "yang"}, {"磊", "lei"}, {"超", "chao"},
        {"晨", "chen"}, {"宁", "ning"}, {"欣", "xin"}, {"燕", "yan"}, {"婷", "ting"},
        {"雪", "xue"}, {"梅", "mei"}, {"兰", "lan"}, {"英", "ying"}, {"琴", "qin"},
        {"洁", "jie"}, {"慧", "hui"}, {"颖", "ying"}, {"琳", "lin"}, {"蕾", "lei"},
    };

    /// <summary>
    /// 将中文姓名转换为拼音
    /// </summary>
    /// <param name="name">中文姓名</param>
    /// <returns>拼音（全小写，无空格）</returns>
    public static string ToPinyin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        var result = new StringBuilder();

        foreach (char c in name)
        {
            string charStr = c.ToString();

            // 首先尝试从姓氏表查找
            if (SurnameMap.TryGetValue(charStr, out var surnamePin yin))
            {
                result.Append(pinyin);
            }
            // 然后从常用字表查找
            else if (CharMap.TryGetValue(charStr, out var charPinyin))
            {
                result.Append(charPinyin);
            }
            // 如果是英文字母或数字，直接添加
            else if (char.IsLetterOrDigit(c))
            {
                result.Append(char.ToLower(c));
            }
            // 其他字符使用简单映射（取首字母）
            else
            {
                // 对于不在映射表中的字，使用Unicode编码的简单映射
                result.Append(GetSimplePinyin(c));
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 简单的拼音映射（基于Unicode范围）
    /// </summary>
    private static string GetSimplePinyin(char c)
    {
        // 中文字符的Unicode范围
        if (c >= 0x4E00 && c <= 0x9FA5)
        {
            // 使用简单的拼音首字母映射（不精确，但可用）
            // 这里使用字符的Unicode值映射到拼音首字母
            int index = (c - 0x4E00) % 23; // 23个声母
            char[] initials = { 'b', 'p', 'm', 'f', 'd', 't', 'n', 'l', 'g', 'k', 'h',
                              'j', 'q', 'x', 'z', 'c', 's', 'r', 'y', 'w', 'zh', 'ch', 'sh' };
            return initials[index].ToString();
        }

        return "x"; // 默认返回x
    }

    /// <summary>
    /// 生成密码：姓名拼音 + 身份证后4位
    /// </summary>
    /// <param name="name">姓名</param>
    /// <param name="idCardLast4">身份证后4位</param>
    /// <returns>密码</returns>
    public static string GeneratePassword(string name, string idCardLast4)
    {
        var pinyin = ToPinyin(name);
        return $"{pinyin}{idCardLast4}";
    }

    /// <summary>
    /// 添加自定义拼音映射
    /// </summary>
    public static void AddCustomMapping(string character, string pinyin)
    {
        CharMap[character] = pinyin.ToLower();
    }
}
