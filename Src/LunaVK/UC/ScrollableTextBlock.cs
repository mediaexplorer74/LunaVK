using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using LunaVK.Core.Utils;
using LunaVK.Core;
using Lunavk.rumon;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Text;
using System;

namespace LunaVK.UC
{
    //NewsTextItem
    //BrowserNavigationService
    public class ScrollableTextBlock : StackPanel
    {
#region Text
        public string Text
        {
            get { return (string)base.GetValue(ScrollableTextBlock.TextProperty); }
            set { base.SetValue(ScrollableTextBlock.TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ScrollableTextBlock), new PropertyMetadata("", new PropertyChangedCallback(ScrollableTextBlock.OnTextPropertyChanged)));

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollableTextBlock)d).OnTextChanged((string)e.NewValue, false);
        }
#endregion

#region Foreground
        public Brush Foreground
        {
            get { return (Brush)base.GetValue(ScrollableTextBlock.BrushProperty); }
            set { base.SetValue(ScrollableTextBlock.BrushProperty, value); }
        }

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ScrollableTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(ScrollableTextBlock.OnForegroundPropertyChanged)));

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((ScrollableTextBlock)d).Children.Count == 0)
                return;

            (((ScrollableTextBlock)d).Children[0] as RichTextBlock).Foreground = (Brush)e.NewValue;
        }
#endregion

        #region FullOnly
        public bool FullOnly
        {
            get { return (bool)GetValue(FullProperty); }
            set { base.SetValue(FullProperty, value); }
        }

        public static readonly DependencyProperty FullProperty = DependencyProperty.Register("FullOnly", typeof(bool), typeof(ScrollableTextBlock), new PropertyMetadata(null));
        #endregion

#region DisableHyperlinks
        public bool DisableHyperlinks
        {
            get { return (bool)GetValue(DisableHyperlinksProperty); }
            set { base.SetValue(DisableHyperlinksProperty, value); }
        }

        public static readonly DependencyProperty DisableHyperlinksProperty = DependencyProperty.Register("DisableHyperlinks", typeof(bool), typeof(ScrollableTextBlock), new PropertyMetadata(null));
#endregion

#region SelectionEnabled
        public bool SelectionEnabled
        {
            get { return (bool)GetValue(SelectionEnabledProperty); }
            set { base.SetValue(SelectionEnabledProperty, value); }
        }

        public static readonly DependencyProperty SelectionEnabledProperty = DependencyProperty.Register("SelectionEnabled", typeof(bool), typeof(ScrollableTextBlock), new PropertyMetadata(null));
#endregion

        /// <summary>
        /// Размер шрифта
        /// Поумолчанию это FontSizeContent
        /// </summary>
        public double FontSize = (double)Application.Current.Resources["FontSizeContent"];

        #region OverrideNavigationTarget
        public string OverrideNavigationTarget
        {
            get { return (string)GetValue(OverrideNavigationTargetProperty); }
            set { SetValue(OverrideNavigationTargetProperty, value); }
        }

        public static readonly DependencyProperty OverrideNavigationTargetProperty = DependencyProperty.Register("OverrideNavigationTarget", typeof(string), typeof(ScrollableTextBlock), new PropertyMetadata(null));
        #endregion

        #region RawJson
        public string RawJson
        {
            get { return (string)GetValue(RawJsonProperty); }
            set { SetValue(RawJsonProperty, value); }
        }

        public static readonly DependencyProperty RawJsonProperty = DependencyProperty.Register("RawJson", typeof(string), typeof(ScrollableTextBlock), new PropertyMetadata(null));
        #endregion

        private void OnTextChanged(string value, bool show_full)
        {
            base.Children.Clear();

            if (string.IsNullOrEmpty(value))
                return;

            if (this.FullOnly)
                show_full = true;

            bool _showReadFull = false;

            if (!show_full)
                value = UIStringFormatterHelper.CutTextGently(value, 300);

            if (value != this.Text)
            {
                value += "...";
                _showReadFull = true;
            }

            RichTextBlock text_block = new RichTextBlock() { IsTextSelectionEnabled = this.SelectionEnabled, FontSize = this.FontSize };
            if (this.Foreground != null)
                text_block.Foreground = this.Foreground;
            else
            {
                text_block.Style = (Style)Application.Current.Resources["RichTextBlockTheme"];
                text_block.ContextMenuOpening += Text_block_ContextMenuOpening;
            }

            bool disableHyperlinks = this.DisableHyperlinks;

            Paragraph par = new Paragraph();

            foreach (string str1 in BrowserNavigationService.ParseText(BrowserNavigationService.PreprocessTextForGroupBoardMentions(value)))
            {
                string[] innerSplit = str1.Split('\b');
                if (innerSplit.Length == 1)
                    BrowserNavigationService.AddRawText(text_block, par, innerSplit[0]);
                else if (innerSplit.Length > 1)
                {
                    if (disableHyperlinks)
                    {
                        BrowserNavigationService.AddRawText(text_block, par, innerSplit[1]);
                    }
                    else
                    {
                        if (innerSplit[0].Contains(BrowserNavigationService._searchFeedPrefix))
                        {
                            int num = innerSplit[0].IndexOf(BrowserNavigationService._searchFeedPrefix) + BrowserNavigationService._searchFeedPrefix.Length;
                            string str2 = innerSplit[0].Substring(num);
                            innerSplit[0] = innerSplit[0].Substring(0, num) + str2;
                        }

                        string overrideTarget = this.OverrideNavigationTarget;
                        try { Debug.WriteLine($"ScrollableTextBlock: OverrideNavigationTarget='{overrideTarget}' tag='{innerSplit[0]}' text='{innerSplit[1]}'"); } catch { }

                        string navTag = !string.IsNullOrEmpty(overrideTarget) ? overrideTarget : innerSplit[0];

                        // If RawJson available, encode it into the nav tag so NavigateOnHyperlink can inspect it
                        string tagForNavigation = navTag;
                        try
                        {
                            if (!string.IsNullOrEmpty(this.RawJson))
                            {
                                var rawBytes = Encoding.UTF8.GetBytes(this.RawJson);
                                string rawB64 = Convert.ToBase64String(rawBytes);
                                tagForNavigation = "<<NAV>>" + navTag + "<<RAW>>" + rawB64;
                            }
                        }
                        catch { }

                        Hyperlink hyperlink = BrowserNavigationService.GenerateHyperlink(innerSplit[1], tagForNavigation, ((h, navstr) =>
                        {
                            BrowserNavigationService.NavigateOnHyperlink(navstr);
                        }), text_block.Foreground);

                        ToolTip toolTip = new ToolTip();
                        string tipContent = innerSplit[0];
                        if (!string.IsNullOrEmpty(navTag))
                        {
                            try
                            {
                                string t = navTag.Replace("\\/", "/").Replace("&amp;", "&").Trim();
                                if (t.StartsWith("vk.ru", System.StringComparison.OrdinalIgnoreCase))
                                    t = "https://" + t;
                                tipContent = t;
                            }
                            catch { tipContent = navTag; }
                        }
                        toolTip.Content = tipContent;
                        ToolTipService.SetToolTip(hyperlink, toolTip);

                        par.Inlines.Add(hyperlink);
                    }
                }
            }

            text_block.Blocks.Add(par);
            base.Children.Add(text_block);

            if (!show_full)
            {
                if (_showReadFull)
                {
                    Border border1 = new Border();

                    TextBlock textBlock1 = new TextBlock();
                    textBlock1.FontWeight = Windows.UI.Text.FontWeights.Medium;
                    textBlock1.Text = LocalizedStrings.GetString("ExpandText");
                    textBlock1.Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
                    textBlock1.FontSize = this.FontSize;

                    border1.Child = textBlock1;
                    border1.Tapped += TextBlockReadFull_OnTap;
                    base.Children.Add(border1);
                }
            }
        }

        private void Text_block_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            RichTextBlock tb = sender as RichTextBlock;
            e.Handled = string.IsNullOrEmpty(tb.SelectedText);
        }

/*
private void ParseText(string value, bool show_full)
{
   if (this.FullOnly)
       show_full = true;

   bool _showReadFull = false;
   //
   if (value == null)
       value = "";
   this.Children.Clear();

   if (!show_full)
       value = UIStringFormatterHelper.CutTextGently(value, 300);

   if (value != this.Text)
   {
       value += "...";
       _showReadFull = true;
   }

   RichTextBlock richTextBox = new RichTextBlock() { IsTextSelectionEnabled = this.SelectionEnabled, FontSize = this.FontSize };
   if (this.Foreground != null)
       richTextBox.Foreground = this.Foreground;
   else
   {
       richTextBox.Style = (Style)Application.Current.Resources["RichTextBlockTheme"];
   }

   Paragraph paragraph = new Paragraph();
   string[] splitResult = linksRegex.Split(value);//_regex_Uri
   foreach (string block in splitResult)
   {
       if (String.IsNullOrEmpty(block)) continue;
       if (block.StartsWith("http", StringComparison.OrdinalIgnoreCase))
       {
           Uri temp = null;
           if (Uri.TryCreate(block, UriKind.Absolute, out temp))
           {
               Hyperlink hp = new Hyperlink();
               hp.Click += (sender, arg) =>
               {
                   Library.NavigatorImpl.Instance.NavigateToWebUri(block);
               };

               hp.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["PhoneAccentColor"]);

               string str = block;
               if (str.Length > 60)
               {
                   str = block.Substring(0, 60);
                   str += "...";
               }
               hp.Inlines.Add(new Run { Text = str });
               paragraph.Inlines.Add(hp);
           }
           else
           {
               paragraph.Inlines.Add(new Run { Text = block });
           }

       }
       else if (block.StartsWith("[", StringComparison.OrdinalIgnoreCase) && block.EndsWith("]", StringComparison.OrdinalIgnoreCase))
       {
           string part = block.Replace("[", "").Replace("]", "");

           Hyperlink hp = new Hyperlink();
           hp.Click += (sender, arg) =>
           {
               string temp = part.Split(new char[] { '|' })[0];
               if (temp.Contains("club"))
               {
                   int id = int.Parse(temp.Replace("club", ""));
                   Library.NavigatorImpl.Instance.NavigateToProfilePage(-id);
               }
               else if (temp.Contains("id"))
               {
                   int id = int.Parse(temp.Replace("id", ""));
                   Library.NavigatorImpl.Instance.NavigateToProfilePage(id);
               }
           };

           hp.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["PhoneAccentColor"]);

           string[] temp2 = part.Split(new char[] { '|' });
           if (temp2.Length > 1)
               hp.Inlines.Add(new Run { Text = temp2[1] });
           else
               hp.Inlines.Add(new Run { Text = part });
           paragraph.Inlines.Add(hp);
       }
       else if (block.StartsWith("#"))
       {
           Hyperlink hp = new Hyperlink();
           hp.Click += (sender, arg) =>
           {
               Library.NavigatorImpl.Instance.NavigateToWebUri("vk.ru/feed?section=search&q=" + block);
           };

           hp.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["PhoneAccentColor"]);
           hp.Inlines.Add(new Run { Text = block });
           paragraph.Inlines.Add(hp);
       }
       else if (block.StartsWith("vk.me", System.StringComparison.OrdinalIgnoreCase) || block.StartsWith("vk.cc", System.StringComparison.OrdinalIgnoreCase))
       {
           Hyperlink hp = new Hyperlink();
           hp.Click += (sender, arg) =>
           {
               Library.NavigatorImpl.Instance.NavigateToWebUri(block);
           };

           hp.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["PhoneAccentColor"]);
           hp.Inlines.Add(new Run { Text = block });
           paragraph.Inlines.Add(hp);
       }
       else
       {
           paragraph.Inlines.Add(new Run { Text = block });
       }
   }
   richTextBox.Blocks.Add(paragraph);
   base.Children.Add(richTextBox);

   if (!show_full)
   {
       if (_showReadFull)
       {
           Border border1 = new Border();

           string str = string.Format("{0}...", "Показать полностью");
           TextBlock textBlock1 = new TextBlock();
           textBlock1.FontWeight = Windows.UI.Text.FontWeights.Bold;
           textBlock1.Text = str;
           textBlock1.Style = (Style)Application.Current.Resources["TextBlockThemeHigh"];
           textBlock1.FontSize = this.FontSize;

           border1.Child = textBlock1;
           border1.Tapped += TextBlockReadFull_OnTap;
           base.Children.Add(border1);
       }
   }
}
*/

        private void SetAppleEmoji(Paragraph paragraph, string block)
        {
            // placeholder if needed
            paragraph.Inlines.Add(new Run { Text = block });
        }

        private void SetSkypeEmoji(Paragraph paragraph, string block)
        {
            paragraph.Inlines.Add(new Run { Text = block });
        }

        void TextBlockReadFull_OnTap(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            this.OnTextChanged(this.Text, true);
        }
    }
}

