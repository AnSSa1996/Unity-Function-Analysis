using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLibCs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class InvalidHelper : MonoBehaviour
{
    public string FunctionName = "A";
    public Regex CallPattern = new Regex(@"\bA\s*\((.*?)\)", RegexOptions.Singleline);

    public UnityEvent<string, object[]> CheckAction;

    public void Awake()
    {
        DontDestroyOnLoad(this);
    }

    [ContextMenu("Fix Invalid")]
    public void FindACallReferences()
    {
        // Scripts 폴더 경로
        CallPattern = new Regex(@"\b" + FunctionName + @"\s*\((.*?)\)", RegexOptions.Singleline);
        var scriptsFolderPath = Path.Combine(Application.dataPath, "Scripts");
        var csFiles = Directory.GetFiles(scriptsFolderPath, "*.cs", SearchOption.AllDirectories);

        foreach (var filePath in csFiles)
        {
            var code = File.ReadAllText(filePath);

            // (...) 호출 구문을 모두 찾음
            var matches = GetMatchesExcludingComments(code, CallPattern);
            if (matches.Count == 0) continue;
            Debug.Log($"함수 호출 발견: {filePath}");
            foreach (var match in matches)
            {
                if (match.IsNull()) return;
                var argsContent = match.Groups[1].Value.Trim();
                var arguments = RemoveQuoteArguments(argsContent);

                // CheckValidate 함수 호출
                if (CheckAction == null) return;
                CheckAction.Invoke(filePath, arguments);
            }
        }
    }

    // 주석 내부를 무시하고 함수 호출 매칭 처리
    private List<Match> GetMatchesExcludingComments(string code, Regex pattern)
    {
        var matches = new List<Match>();

        // 주석 탐지 정규식
        var commentPattern = new Regex(@"//.*|/\*[\s\S]*?\*/");

        // 주석 매칭
        var commentMatches = commentPattern.Matches(code);
        var commentRanges = commentMatches.Cast<Match>().Select(m => new Range(m.Index, m.Index + m.Length)).ToList();

        // 함수 호출 매칭
        var functionMatches = pattern.Matches(code);

        foreach (Match match in functionMatches)
        {
            // 함수 호출이 주석 내부에 있는지 확인
            var isInComment = commentRanges.Any(range => match.Index >= range.Start.Value && match.Index <= range.End.Value);
            if (isInComment) continue;

            // 매개변수 필터링
            var argsContent = match.Groups[1].Value.Trim();
            var arguments = SplitArguments(argsContent); // 매개변수를 ',' 기준으로 분리

            // 리터럴이 아닌 항목 포함 여부 확인
            if (AllArgumentsAreLiterals(arguments) == false) continue;

            matches.Add(match);
        }

        return matches;
    }

    // 모든 매개변수가 리터럴인지 확인
    private bool AllArgumentsAreLiterals(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (IsLiteral(argument) == false) return false;
        }

        return true;
    }

    // 매개변수가 리터럴인지 확인
    private bool IsLiteral(string argument)
    {
        // 리터럴 패턴 (문자열, 숫자, 불리언, null)
        var literalPattern = new Regex(@"^(" + 
                                       @"""[^""]*""|" +     // 문자열 리터럴, 큰따옴표로 감싸여 있음
                                       @"\b\d+(\.\d+)?\b|" + // 숫자 리터럴(정수 또는 실수)
                                       @"\btrue\b|\bfalse\b|" + // 불리언 리터럴
                                       @"\bnull\b" +
                                       @")$");

        return literalPattern.IsMatch(argument.Trim());
    }


    // 간단한 인자 파싱: 
    // 쉼표를 기준으로 분리하되, 문자열 내부 처리나 복잡한 경우 고려하지 않음(단순 예제)
    private string[] SplitArguments(string argsContent)
    {
        // 만약 괄호 안에 아무 인자도 없다면 빈 배열 반환
        if (string.IsNullOrWhiteSpace(argsContent))
        {
            return new string[0];
        }

        // 쉼표로 단순 분리
        // 실제 코드에서는 문자열 리터럴, 괄호 중첩 등의 상황이 발생할 수 있으므로 더욱 정교한 파싱이 필요할 수 있음.
        var splitted = argsContent.Split(',');

        for (var i = 0; i < splitted.Length; i++)
        {
            splitted[i] = splitted[i].Trim();
        }

        return splitted;
    }
    
    
    private string[] RemoveQuoteArguments(string argsContent)
    {
        // 만약 괄호 안에 아무 인자도 없다면 빈 배열 반환
        if (string.IsNullOrWhiteSpace(argsContent))
        {
            return new string[0];
        }

        // 쉼표로 단순 분리
        // 실제 코드에서는 문자열 리터럴, 괄호 중첩 등의 상황이 발생할 수 있으므로 더욱 정교한 파싱이 필요할 수 있음.
        var splitted = argsContent.Split(',');

        for (var i = 0; i < splitted.Length; i++)
        {
            splitted[i] = splitted[i].Trim();
            splitted[i] = splitted[i].Trim('"');
        }

        return splitted;
    }

    // CheckValidate 함수: 다양한 형태의 인자를 받을 수 있도록 params object[] 사용
    // 여기서는 단순히 인자 내용을 디버그에 출력.
    private void CheckValidate(params object[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            Debug.Log($"인자 {i + 1} : {args[i]}");
        }

        // 여기서 인자들에 대한 유효성 검사 로직 추가 가능
        // 예: 문자열 패턴 검사, 특정 타입 변환 시도 등
    }

    #region +CUSTOM CHECK ACTION

    #endregion
}