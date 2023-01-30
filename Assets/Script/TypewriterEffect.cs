using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/***
 * 项目：打字机效果Demo
 * 
 * 说明：处理带富文本效果的文本，使具有打字机一样的效果(文本中的字符逐个显示)
 *     
 * 时间：2023-01-30
 */
public class TypewriterEffect : MonoBehaviour
{
    public delegate void OnComplete();

    [SerializeField]
    private float _intervalTime = 0.05f; //字符逐个显示的时间间隔

    private TextMeshPro label;//如果是UI，就换成TextMeshProUGUI类型
    private string _currentText;//当前正在显示的文本
    private string _finalText;
    private Coroutine _typeTextCoroutine;

    private static readonly string[] _uguiSymbols = { "b", "i" };
    private static readonly string[] _uguiCloseSymbols = { "b", "i", "size", "color" };
    private OnComplete _onCompleteCallback;
    Richtext _richText;
    private void Start()
    {
        label = GetComponent<TextMeshPro>();
        _richText = new Richtext();
        SetText(label.text);
    }

    /// <summary>
    /// 文本以打字机的形式显示
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="time">字符逐个显示的时间间隔</param>
    public void SetText(string text, float time = -1)
    {
        _intervalTime = time > 0 ? time : _intervalTime;
        _finalText = ReplaceTime(text);
        label.text = "";

        if (_typeTextCoroutine != null)
        {
            StopCoroutine(_typeTextCoroutine);
        }

        _typeTextCoroutine = StartCoroutine(TypeText(text));
    }

    public void SkipTypeText()
    {
        if (_typeTextCoroutine != null)
            StopCoroutine(_typeTextCoroutine);
        _typeTextCoroutine = null;

        label.text = _finalText;

        if (_onCompleteCallback != null)
            _onCompleteCallback();
    }

    /// <summary>
    /// 解析带副文本效果的文本内容
    /// </summary>
    /// <param name="text">带富文本效果的文本内容</param>
    /// <returns></returns>
    public IEnumerator TypeText(string text)
    {
        _currentText = "";
        var len = text.Length;
        float time = _intervalTime;      
        _richText.ReSet();
        for (var i = 0; i < len; i++)
        {
            //解析要间隔的时间，把速度内容跳过
            if (text[i] == '[' && i + 5 < len && text.Substring(i, 6).Equals("[time="))
            {
                var parseTime = "";
                for (var j = i + 6; j < len; j++)
                {
                    if (text[j] == ']')
                        break;
                    parseTime += text[j];
                }

                if (!float.TryParse(parseTime, out time))
                    time = 0.05f;

                i += 7 + parseTime.Length - 1;
                continue;
            }

            // ngui color tag
            if (text[i] == '[' && i + 7 < len && text[i + 7] == ']')
            {
                _currentText += text.Substring(i, 8);
                i += 8 - 1;
                continue;
            }

            //该字符是否是富文本
            bool isRichText = false;

            //是否是单个字母的富文本效果，如：粗体<b>、斜体</i>
            for (var j = 0; j < _uguiSymbols.Length; j++)
            {
                var symbol = string.Format("<{0}>", _uguiSymbols[j]);
                if (text[i] == '<' && i + (1 + _uguiSymbols[j].Length) < len && text.Substring(i, 2 + _uguiSymbols[j].Length).Equals(symbol))
                {
                    _currentText += symbol;
                    i += (2 + _uguiSymbols[j].Length) - 1;
                    isRichText = true;                 
                    if (_uguiSymbols[j] == RichTextType.b.ToString())
                    {
                        _richText.ChangeOpenState(RichTextType.b, true);
                    }
                    else if (_uguiSymbols[j] == RichTextType.i.ToString())
                    {
                        _richText.ChangeOpenState(RichTextType.i, true);
                    }

                    break;
                }
            }

            //是否是颜色color富文本:#号后面必须是8位颜色代码，即包含透明度
            if (text[i] == '<' && i + (1 + 15) < len && text.Substring(i, 2 + 6).Equals("<color=#") && text[i + 16] == '>')
            {
                _currentText += text.Substring(i, 2 + 6 + 8 + 1);
                i += (2 + 14) - 1 + 1;
                isRichText = true;              
                _richText.ChangeOpenState(RichTextType.color, true);
            }

            //是否是大小size富文本
            if (text[i] == '<' && i + 5 < len && text.Substring(i, 6).Equals("<size="))
            {
                var parseSize = "";
                var size = (float)label.fontSize;
                for (var j = i + 6; j < len; j++)
                {
                    if (text[j] == '>') break;
                    parseSize += text[j];
                }

                if (float.TryParse(parseSize, out size))
                {
                    _currentText += text.Substring(i, 7 + parseSize.Length);
                    i += (7 + parseSize.Length) - 1;
                    isRichText = true;                
                    _richText.ChangeOpenState(RichTextType.size, true);
                }
            }

            // exit symbol
            //读取富文本的关闭字段
            for (var j = 0; j < _uguiCloseSymbols.Length; j++)
            {
                var symbol = string.Format("</{0}>", _uguiCloseSymbols[j]);
                if (text[i] == '<' && i + (2 + _uguiCloseSymbols[j].Length) < len && text.Substring(i, 3 + _uguiCloseSymbols[j].Length).Equals(symbol))
                {
                    _currentText += symbol;
                    i += (3 + _uguiCloseSymbols[j].Length) - 1;
                    isRichText = true;                   
                    _richText.ChangeOpenState(_uguiCloseSymbols[j], false);
                    //break;
                }
            }
            //如果是富文本(富文本的内容前面已经添加上)
            if (isRichText)
            {
                //如果富文本不是闭合的
                if (!_richText.GetIsClose())
                {
                    //就继续遍历下一个字符
                    continue;
                }
                //如果富文本是闭合的，就显示出来
                label.text = _currentText;
            }
            else//不是副文本，是字符
            {
                //拼接当前字符串
                _currentText += text[i];
                label.text = _currentText;
                //等待显示下一个字符
                yield return new WaitForSeconds(time);
            }
            ////label.text = _currentText + (tagOpened ? string.Format("</{0}>", tagType) : "");
            //label.text = _currentText/* + (_richText.GetIsClose() ? string.Format("</{0}>", tagType) : "")*/;
            //yield return new WaitForSeconds(speed);
        }

        _typeTextCoroutine = null;

        if (_onCompleteCallback != null)
            _onCompleteCallback();
    }

    private string ReplaceTime(string text)
    {
        var result = "";
        var len = text.Length;
        for (var i = 0; i < len; i++)
        {
            if (text[i] == '[' && i + 5 < len && text.Substring(i, 6).Equals("[time="))
            {
                var speedLength = 0;
                for (var j = i + 6; j < len; j++)
                {
                    if (text[j] == ']')
                        break;
                    speedLength++;
                }

                i += 7 + speedLength - 1;
                continue;
            }

            result += text[i];
        }

        return result;
    }

    public bool IsSkippable()
    {
        return _typeTextCoroutine != null;
    }

    public void SetOnComplete(OnComplete onComplete)
    {
        _onCompleteCallback = onComplete;
    }
}
public class Richtext
{
    bool _isSizeOpen = false;//字体大小
    bool _isColorOpen = false;//颜色
    bool _isB = false;//加粗
    bool _isI = false;//斜体

    public void ChangeOpenState(RichTextType type, bool isOpen)
    {
        switch (type)
        {
            case RichTextType.size:
                _isSizeOpen = isOpen;
                break;

            case RichTextType.color:
                _isColorOpen = isOpen;
                break;

            case RichTextType.b:
                _isB = isOpen;
                break;

            case RichTextType.i:
                _isI = isOpen;
                break;
        }
    }

    public void ChangeOpenState(string type, bool isOpen)
    {
        RichTextType ownType = (RichTextType)Enum.Parse(typeof(RichTextType), type);
        ChangeOpenState(ownType, isOpen);
    }
   

    public void ReSet()
    {
        _isSizeOpen = false;
        _isColorOpen = false;
        _isB = false;
        _isI = false;
    }

    /// <summary>
    /// 获取该字段所使用的所有富文本是否都关闭了
    /// </summary>
    /// <returns></returns>
    public bool GetIsClose()
    {
        if (!_isSizeOpen && !_isColorOpen && !_isB && !_isI)
        {
            return true;
        }
        return false;
    }
}

/// <summary>
/// 富文本的类型
/// </summary>
public enum RichTextType
{
    color,//颜色
    size,//大小
    b,//加粗
    i//斜体
}
