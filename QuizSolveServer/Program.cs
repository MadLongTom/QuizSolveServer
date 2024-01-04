// See https://aka.ms/new-console-template for more information

using QuizSolveServer;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using Console = Colorful.Console;
using System.Drawing;
using System.Diagnostics;
using System.IO.Hashing;
using System.Linq.Expressions;
using System.Linq;

Console.Title = "QuizSolveServer";
Console.BackgroundColor = Color.FromArgb(30, 30, 30);
Console.ForegroundColor = Color.LightPink;
Console.Clear();

/*Task.Factory.StartNew(() =>
{
    Random rd = new Random();
    while (true)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            sb.Append((char)((int)'0' + rd.Next(0, 10)));
        }
        Console.Title = sb.ToString();
        Thread.Sleep(1000);
    }
});*/

if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
if (!Directory.Exists("dst")) Directory.CreateDirectory("dst");
if (!Directory.Exists("exam")) Directory.CreateDirectory("exam");

try
{
    Process pp = new();
    pp.StartInfo.FileName = "pip";
    pp.StartInfo.CreateNoWindow = true;
    pp.StartInfo.RedirectStandardError = true;
    pp.StartInfo.RedirectStandardInput = true;
    pp.StartInfo.RedirectStandardOutput = true;
    pp.StartInfo.UseShellExecute = false;
    pp.StartInfo.Arguments = "list";
    pp.Start();
    string va = pp.StandardOutput.ReadToEnd();
    if (!(va.Contains("selenium-wire") && va.Contains("webdriver-manager")))
    {
        throw new Exception();
    }
    pp.Kill();
}
catch (Exception ex)
{
    Environment.Exit(0);
}


int sID = 0;

Console.WriteLine("请输入连接的频道：" + Environment.NewLine + "1 - 工科数学分析2" + Environment.NewLine + "2 - 概率论与数理统计");

margorPConfig[] mgpc = new margorPConfig[]
{
};
int result;
LABEL1:
if (!int.TryParse(Console.ReadLine(), out result))
{
    Console.WriteLine("输入的格式有误，请重新输入：");
    goto LABEL1;
}
if (result < 1 || result > mgpc.Length)
{
    Console.WriteLine("输入的数据有误，请重新输入：");
    goto LABEL1;
}

Task.Factory.StartNew(() =>
{
    FileSystemWatcher watcher = new("dst")
    {
        EnableRaisingEvents = true
    };
    while (true)
    {
        string watcherfile = watcher.WaitForChanged(WatcherChangeTypes.Changed).Name;
        UpLoad.UpLoadFiles.UploadFile(Path.Combine(Environment.CurrentDirectory, "dst", watcherfile), Path.Combine(result.ToString(), "dst"), watcherfile);
    }
});

Task.Factory.StartNew(() =>
{
    FileSystemWatcher watcher = new("exam")
    {
        EnableRaisingEvents = true
    };
    while (true)
    {
        string watcherfile = watcher.WaitForChanged(WatcherChangeTypes.Changed).Name;
        UpLoad.UpLoadFiles.UploadFile(Path.Combine(Environment.CurrentDirectory, "exam", watcherfile), Path.Combine(result.ToString(), "exam"), watcherfile);
    }
});

margorP mgp = new(mgpc[result - 1]);
Console.WriteLine("Connected");
string receive = null;
string KEY = null;
#if RELEASE
if (mgp.AppEn_v() != "v1.5")
{
    Console.WriteLine("请联系上级代理获取新版软件");
    Console.ReadKey();
    Environment.Exit(0);
}

do
{
    if (receive != null)
    {
        Console.WriteLine($"卡密有误，请重新输入：（错误原因：{receive}）");
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine(mgp.AppEn_GetGongGao());
        Console.WriteLine();
        Console.WriteLine("请输入卡密：");
    }
    KEY = Console.ReadLine();
    receive = mgp.AppEn_LogInKV(KEY, "");
} while (!receive.Contains("01|1081"));

string rootpath = receive.Split('|')[3].Split(',')[0];
string guipath = receive.Split('|')[3].Split(',')[1];
string crc = receive.Split('|')[3].Split(',')[2];
bool autoRoute = false,autoSubmit = false;
Console.WriteLine("是否启用自动填写模式？若是请输入1回车，否则为否");
if (Console.ReadLine() == "1") guipath = guipath.Replace("GUI.py", "GUIA.py");
Console.WriteLine("是否启用自动寻路模式？（适用于全包情况）若是请输入1回车，否则为否");
if (Console.ReadLine() == "1") autoRoute = true;
Console.WriteLine("是否启用自动提交模式？（适用于除期末考试情况）若是请输入1回车，否则为否");
if (Console.ReadLine() == "1") autoSubmit = true;
Console.WriteLine(mgp.AppEn_PointsDeduction(KEY, 1) == "90010" ? "点数已被扣除1" : $"点数扣除失败");
#else
#endif

HttpClient client = new();
string filename = "active";
JObject root = JObject.Parse(Decrypt(client.GetAsync(rootpath).Result.Content.ReadAsStringAsync().Result, mgp.AppEn_GetLogicText_A(), mgp.AppEn_GetLogicText_B()));
string pyname = Guid.NewGuid().ToString().Replace('{', 'A').Replace('}', 'A') + ".py";
bool isNewPaper = false;

Console.WriteLine("登录成功，正在启动浏览器环境");
Process p = null;
Task.Factory.StartNew(() =>
{
    Crc32 c = new();
    c.Append(File.ReadAllBytes(@"streamload.py"));
    if (crc != Convert.ToBase64String(c.GetCurrentHash()))
    {
        Environment.Exit(0);
    }
#if RELEASE
    File.WriteAllText(pyname, Decrypt(client.GetAsync(guipath).Result.Content.ReadAsStringAsync().Result, mgp.AppEn_GetLogicText_A(), mgp.AppEn_GetLogicText_B()));
#else
#endif
    ProcessStartInfo psi = new()
    {
        UseShellExecute = false,
        RedirectStandardInput = true,
        //psi.StandardInputEncoding = Encoding.UTF8;
        FileName = "python",
        Arguments = $"streamload.py {pyname} os.remove(sys.argv[1])"
    };
    p = Process.Start(psi);
    p.StandardInput.AutoFlush = true;
    p.WaitForExit();
    DirectoryInfo di2 = new("temp");
    foreach (FileInfo file in di2.GetFiles())
    {
        try
        {
            File.Delete(file.FullName);
        }
        catch (Exception) { }
    }
    Environment.Exit(0);
});

bool intime = false;
//while (p == null) { }
bool isFullyResolved = false;
Console.WriteLine("[LoaderModule] - Info - Loaded");
int[] qID = null;

FileSystemWatcher watcher = new(Path.Combine(Environment.CurrentDirectory, "temp"))
{
    EnableRaisingEvents = true
};

while (true)
{
    string fn;
    do
    {
        fn = Path.Combine(Environment.CurrentDirectory, "temp", watcher.WaitForChanged(WatcherChangeTypes.Changed).Name);
#if DEBUG
        //Console.WriteLine("[ShareModule] - Info - Received "+fn);
#endif
    } while (!(fn.Contains(filename) || fn.Contains("question") || fn.Contains("plan")));
    if (fn.Contains(filename))
    {
        Task.Factory.StartNew(() => ParseSheet(fn));
    }
    else if (fn.Contains("question"))
    {
        Task.Factory.StartNew(() => ParseQuestion(fn));
    }
    else if (fn.Contains("plan"))
    {
        if (autoRoute)
            Task.Factory.StartNew(() => ParsePlan(fn));
    }
    /*try
    {
        File.Delete(fn);
    }
    catch (Exception ex) { }*/

}

void ParsePlan(string fn)
{
    try
    {
        if (intime) { return; }
        intime = true;

        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(5000);
            intime = false;
        });
        JObject o1 = JObject.Parse(File.ReadAllText(fn));
        JArray o2 = o1["rt"] as JArray;
        int paperCount = 0;
        bool paperRemains = false;
        foreach (JObject o3 in o2.Cast<JObject>())
        {
            if ((int)o3["floorType"] != 0)
            {
                //enter test section
                if ((float?)o3["bestGrades"] == null | (float?)o3["bestGrades"] < 100)
                {
                    long currentTimeStamp = Convert.ToInt64(DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds * 1000);
                    long startTimeStamp = (long)o3["startTime"];
                    long endTimeStamp = (long)o3["endTime"];
                    if (currentTimeStamp > startTimeStamp)
                    {
                        //enter paper
                        p.StandardInput.WriteLine("enter " + paperCount);
                        Console.WriteLine("[RouteModule] - Info - Enter Paper " + paperCount);
                        paperRemains = true;
                        break;
                    }
                }
            }
            paperCount++;
        }
        if (!paperRemains)
        {
            int sectionCount = 0;
            foreach (JObject o3 in o2.Cast<JObject>())
            {
                int childCount = 0;
                if ((int)o3["floorType"] == 0)
                {
                    //enter point section
                    if (SearchChildren(o3, ref childCount, sectionCount)) break;
                }
                sectionCount++;
            }
        }


    }
    catch (Exception ex)
    {

    }
    try
    {
        File.Delete(fn);
    }
    catch (Exception ex)
    {

    }
}

bool SearchChildren(JObject o, ref int count, int section)
{
    JArray j = o["children"] as JArray;
    if (j.Count != 0)
    {
        foreach (JObject child in j)
        {
            if (SearchChildren(child, ref count, section))
                return true;
        }
        return false;
    }
    else
    {
        if ((int)o["freeExam"] == 0 && ((int?)o["mastery"] == null | (int?)o["mastery"] <= 80))
        {

            //enter paper
            p.StandardInput.WriteLine($"enterc {section} {count}");
            Console.WriteLine("[RouteModule] - Info - Enter Point U" + section + "P" + count);
            return true;
        }
        else
        {
            count++;
            return false;
        }
    }
}

void ParseSheet(string fn)
{
    try
    {
        JObject o1 = JObject.Parse(File.ReadAllText(fn));
        if ((int)o1["data"]["partSheetVos"][0]["id"] == sID) goto LABEL2;
        sID = (int)o1["data"]["partSheetVos"][0]["id"];
        JArray qSV = o1["data"]["partSheetVos"][0]["questionSheetVos"] as JArray;
        int count = 0;
        qID = new int[qSV.Count];
        foreach (var (q, ans) in from JObject q in qSV.Cast<JObject>()
                                 let ans = GetAnswer((int)q["questionId"])
                                 select (q, ans))
        {
            if (ans != null)
            {
                //Console.WriteLine($"{(int)q["questionId"]} Found, Right Answers are {PrintArray(GetRightAnswers(ans))}");
                qID[count] = (int)q["questionId"];
            }
            else
            {
                //Console.WriteLine($"{(int)q["questionId"]} Not Found.");
            }

            count += 1;
        }

        Console.WriteLine($"[PaperModule] - Info - {qID.Count(q => q != 0)}/{qSV.Count} Resolved.");
        if (qID.Count(q => q != 0) == qSV.Count) isFullyResolved = true; else isFullyResolved = false;
        isNewPaper = true;
        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(5000);
            isNewPaper = false;
            p.StandardInput.WriteLine("startanswer");
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error opening sheet");
    }
    LABEL2:
    try
    {
        File.Delete(fn);
    }
    catch (Exception) { }
}

void ParseQuestion(string fn)
{
    try
    {
        if (isNewPaper)
        {
            Console.WriteLine($"[AnswerModule] - Info - Abort this operation, new paper loaded.");
            goto LABEL3;
        }
        JObject o1 = JObject.Parse(File.ReadAllText(fn));
        int questionid = (int)o1["data"]["id"];
        JArray qSV = o1["data"]["optionVos"] as JArray;
        JObject ans = GetAnswer(questionid);
        int qiD = 0;
        qID ??= new int[] { };
        for (int i = 0; i < qID.Length; i++)
        {
            if (qID[i] == questionid)
            {
                qiD = i + 1;
                break;
            }
        }
        if (ans != null)
        {
            int[] rights = GetRightAnswers(ans);
            char opChar = 'A';
            string[] serial = new string[rights.Length];
            int count = 0;
            List<char> answer = new();
            foreach (JObject option in qSV.Cast<JObject>())
            {
                if (rights.Contains((int)option["id"]))
                {
                    serial[count] = ((int)opChar - (int)'A' + 1).ToString();
                    answer.Add(opChar);
                    count++;
                }
                opChar += (char)1;
            }
            string s = string.Join(',', serial);
            string a = string.Join(",", answer);
            Console.WriteLine($"[AnswerModule] - Info - {(qiD == 0 ? questionid : "Question " + qiD)} should choose: {a}");
            p.StandardInput.WriteLine(s);
            if (qiD == qID.Length && isFullyResolved && autoSubmit)
            {
                Thread.Sleep(2000);
                p.StandardInput.WriteLine("submit");
            }
        }
        else
        {
            p.StandardInput.WriteLine("None");
            Console.WriteLine($"No record for {questionid}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error opening question");
    }
    LABEL3:
    try
    {
        File.Delete(fn);
    }
    catch (Exception) { }
}

JObject GetAnswer(int id)
{
    foreach (var q in (root["rt"]["allQuestionList"] as JArray).Cast<JObject>().Where(q => (int)q["id"] == id))
    {
        return q;
    }
    return null;
}

int[] GetRightAnswers(JObject answer)
{
    List<int> answer_ = new();
    answer_.AddRange((answer["questionOptionList"] as JArray).Cast<JObject>().Where(option => (int)option["isCorrect"] == 1).Select(option => (int)option["id"]));
    return answer_.ToArray();
}
string PrintArray(int[] a)//具体数组类型可根据实际更改
{
    //需要对输入数组判断是否为空
    if (a.Length != 0)
    {
        string str = "";
        foreach (var i in a)
        {
            str = str + i + ",";
        }
        return "[" + str[..^1] + "]";//通过Substring()去除对应字符
    }
    else return "[]";//如果数组为空，打印空数组格式即可
}


static string Decrypt(string encryptedString, string key, string iv)
{
    byte[] btKey = Encoding.UTF8.GetBytes(key);
    byte[] btIV = Encoding.UTF8.GetBytes(iv);
    DESCryptoServiceProvider des = new();
    using MemoryStream ms = new();
    byte[] inData = Convert.FromBase64String(encryptedString);
    try
    {
        using (CryptoStream cs = new(ms, des.CreateDecryptor(btKey, btIV), CryptoStreamMode.Write))
        {
            cs.Write(inData, 0, inData.Length);
            cs.FlushFinalBlock();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }
    catch
    {
        return encryptedString;
    }
}

public struct margorPConfig
{
    public string baseurl;
    public string key;
    public string password;
}

public struct Course
{
    public string name;
    public int percentage;
    public bool hasExam;
    public int serial;
}

public struct CoruseUnit
{
    public string name;
    public Course[] courses;
    public int serial;
}

public struct Exam
{
    public string name;
    public int score;
    public int startStamp, endStamp;
    public bool openState;
}

