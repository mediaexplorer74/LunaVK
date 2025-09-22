using LunaVK.Core.Enums;
using LunaVK.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using VkLib.Error; // Added for VkLib exceptions
using Windows.Storage;

namespace LunaVK.Core.Network
{
    public static class VKRequestsDispatcher
    {
        /// <summary>
        /// https://api.vk.ru/method/
        /// </summary>
        public static string RequestUriFrm
        {
            get { return (Settings.UseProxy ? "https://vk-api-proxy.xtrafrancyz.net" : VKConstants.HostURL) + "/method/"; }
        }

        public static void Execute<T>(string code, Action<VKResponse<T>> callback, Func<string, string> customDesFunc = null, bool lowPriority = false, CancellationToken? cancellationToken = null) where T : class
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["code"] = code;
            VKRequestsDispatcher.DispatchRequestToVK<T>("execute", parameters, callback, customDesFunc, lowPriority, cancellationToken);
        }

        /// <summary>
        /// Выполняется на фоновой ветке
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        /// <param name="customDeserializationFunc"></param>
        /// <param name="lowPriority"></param>
        /// <param name="pageDataRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="confirmationRequiredHandler"></param>
        public static void DispatchRequestToVK<R>(string methodName, Dictionary<string, string> parameters, Action<VKResponse<R>> callback, Func<string, string> customDeserializationFunc = null, bool lowPriority = false, CancellationToken? cancellationToken = null)
        {
            VKRequestsDispatcher.DoDispatch<R>(VKRequestsDispatcher.RequestUriFrm + methodName, parameters, callback, customDeserializationFunc, lowPriority, cancellationToken);
        }

        private static void DoDispatch<R>(string baseUrl, Dictionary<string, string> parameters, Action<VKResponse<R>> callback, Func<string, string> customDeserializationFunc = null, bool lowPriority = false, CancellationToken? cancellationToken = null)
        {
            if (!parameters.ContainsKey("v"))
                parameters["v"] = VKConstants.API_VERSION;
            if (!parameters.ContainsKey("access_token") && Settings.AccessToken != null)
                parameters["access_token"] = Settings.AccessToken;
            parameters["lang"] = VKRequestsDispatcher.GetLang;

            //baseUrl = https://api.vk.ru/method/[METHOD]
            JsonWebRequest.SendHTTPRequestAsync(baseUrl, parameters, ((jsonResp, IsSucceeded) =>
            {
                VKResponse<R> response = null;

                if (IsSucceeded)
                {
                    if (customDeserializationFunc != null)
                        jsonResp = customDeserializationFunc(jsonResp);
                    try
                    {
                        response = JsonConvert.DeserializeObject<VKResponse<R>>(jsonResp);
                        if (response.error.error_code != VKErrors.None)
                        {
                            var list = parameters.Where((param) => param.Key != "access_token" && param.Key != "lang" && param.Key != "v").Select((param) => { return param.Key + ":" + param.Value; }).ToList();

                            // Log to persistent logger
                            Logger.Instance.Error(baseUrl + list.GetCommaSeparated() + Environment.NewLine + jsonResp, response.error);

                            // Also write to debug output for immediate visibility during development
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"VK API Error: code={response.error.error_code} method={baseUrl} params={list.GetCommaSeparated()} json={jsonResp}");
                            }
                            catch { }
                        }
                    }
                     catch (Exception e)
                     {
                        response = new VKResponse<R>();
                        response.error.error_msg = e.Message;
                        response.error.error_code = VKErrors.DeserializationError;

                        var list = parameters.Where((param) => param.Key != "access_token" && param.Key != "lang" && param.Key != "v").Select((param) => { return param.Key + ":" + param.Value; }).ToList();

                        Logger.Instance.Error(baseUrl + list.GetCommaSeparated() + Environment.NewLine + jsonResp, response.error);

                        //
                        //
                        //if (Settings.DEV_IsLogsPopupEnabled)
                        MessageDialog dialog = new MessageDialog(e.Message);
                        dialog.Title = "Deserialization error";
                        dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                        var res = dialog.ShowAsync();

                        //
                        //
                    }
                }
                else
                {
                    response = new VKResponse<R>();
                    response.error.error_code = VKErrors.NoNetwork;
                }
                callback?.Invoke(response);
            }), true, lowPriority, cancellationToken);

#if DEBUG
            var list2 = parameters.Where((param) => param.Key != "access_token" && param.Key != "lang" && param.Key != "v").Select((param) => { return param.Key + ":" + param.Value; }).ToList();
            System.Diagnostics.Debug.WriteLine(baseUrl + list2.GetCommaSeparated());
#endif
        }

        /// <summary>
        /// Исправляем ответ сервера [] => {}
        /// </summary>
        /// <param name="jsonStr">Собственно сам текст ответа</param>
        /// <param name="arrayName">Имя массива, который пустым возвращается как []</param>
        /// <returns>Возвращаем исправленный ответ</returns>
        public static string FixArrayToObject(string jsonStr, string arrayName)
        {
            return jsonStr.Replace(arrayName + "\":[]", arrayName + "\":{}");
        }

        /// <summary>
        /// Исправляем ответ сервера
        /// Вида "chat":false,"chat_participants":false
        /// </summary>
        /// <param name="jsonStr">Собственно сам текст ответа</param>
        /// <param name="arrayName">Имя массива, который пустым возвращается как false</param>
        /// <returns>Возвращаем исправленный ответ</returns>
        public static string FixFalseArray(string jsonStr, string arrayName, bool substituteWithCurlyBrackets = false)
        {
            if (!substituteWithCurlyBrackets)
                return jsonStr.Replace(arrayName + "\":false", arrayName + "\":[]");
            return jsonStr.Replace(arrayName + "\":false", arrayName + "\":{}");
        }

        public static string FixNull(string jsonStr, string paramName)
        {
            return jsonStr.Replace(",\""+paramName + "\":null", "");
        }

        public static string GetArrayCountAndRemoveExt(string jsonStr, string arrayName, int startInd, out int resultCount, out int foundInd)
        {
            resultCount = 0;
            int startIndex1 = startInd < jsonStr.Length ? jsonStr.IndexOf("\"" + arrayName + "\":", startInd) : -1;
            foundInd = startIndex1;
            if (startIndex1 < 0)
                return jsonStr;
            int startIndex2 = jsonStr.IndexOf("[", startIndex1);
            if (startIndex2 < 0)
                return jsonStr;
            int val1 = jsonStr.IndexOf(",", startIndex2);
            int num = jsonStr.IndexOf("]", startIndex2);
            if (val1 < 0 && num < 0)
                return jsonStr;
            int val2 = num < 0 ? val1 : num;
            if (val1 > 0)
                val2 = Math.Min(val1, val2);
            if (val2 - startIndex2 <= 1 || !int.TryParse(jsonStr.Substring(startIndex2 + 1, val2 - startIndex2 - 1).Replace("\"", ""), out resultCount))
                return jsonStr;
            if (resultCount < 0)
                resultCount = 0;
            if (val1 > num || val1 == -1)
                return jsonStr.Remove(startIndex2 + 1, val2 - startIndex2 - 1);
            return jsonStr.Remove(startIndex2 + 1, val2 - startIndex2);
        }

        public static string GetArrayCountAndRemove(string jsonStr, string arrayName, out int resultCount)
        {
            int foundInd;
            return GetArrayCountAndRemoveExt(jsonStr, arrayName, 0, out resultCount, out foundInd);
        }

        private static string GetShortLangTag(string fullTag)
        {
            if (string.IsNullOrEmpty(fullTag))
                return "en";
            int dash = fullTag.IndexOf('-');
            if (dash == -1)
                return fullTag.ToLowerInvariant();
            return fullTag.Substring(0, dash).ToLowerInvariant();
        }

        private static string GetLang
        {
            get
            {
                try
                {
                    // 1) Prefer explicit selection from app Settings (SelectedLanguageCode)
                    var saved = Settings.SelectedLanguageCode;
                    if (!string.IsNullOrEmpty(saved))
                        return GetShortLangTag(saved);

                    // 2) Fallback to ApplicationLanguages.PrimaryLanguageOverride
                    var primary = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
                    if (!string.IsNullOrEmpty(primary))
                        return GetShortLangTag(primary);

                    // 3) Use the first available runtime language
                    var langs = Windows.Globalization.ApplicationLanguages.Languages;
                    if (langs != null && langs.Count > 0)
                        return GetShortLangTag(langs[0]);

                    // fallback
                    return "en";
                }
                catch
                {
                    return "en";
                }
            }
        }

        public static void DispatchLoginRequest(string login, string password, Action<VKErrors, string> callback)
        {
            // Use VkLib direct authentication instead of the complex OAuth flow
            Task.Run(async () =>
            {
                try
                {
                    var token = await VkService.Instance.Auth.Login(login, password,
                        VkLib.Auth.VkScopeSettings.CanAccessFriends |
                        VkLib.Auth.VkScopeSettings.CanAccessGroups |
                        VkLib.Auth.VkScopeSettings.CanAccessMessages |
                        VkLib.Auth.VkScopeSettings.CanAccessWall |
                        VkLib.Auth.VkScopeSettings.CanAccessVideos |
                        VkLib.Auth.VkScopeSettings.CanAccessPhotos |
                        VkLib.Auth.VkScopeSettings.CanAccessDocs |
                        VkLib.Auth.VkScopeSettings.CanAccessAudios |
                        VkLib.Auth.VkScopeSettings.Offline);

                    if (token != null)
                    {
                        // Convert VkLib token to LunaVK format
                        string result = $"access_token={token.Token}&expires_in={token.ExpiresIn.Ticks}&user_id={token.UserId}";
                        // Replace this line inside DispatchLoginRequest:
                        // Execute.ExecuteOnUIThread(() => callback(VKErrors.None, result));

                        // With this line:
                        Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.None, result));
                        //Execute.ExecuteOnUIThread(() => callback(VKErrors.None, result));
                    }
                    else
                    {
                        Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.AccessDenied, "Не удалось получить токен доступа"));
                    }
                }
                catch (VkCaptchaNeededException ex)
                {
                    Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.CaptchaNeeded, $"Требуется капча: {ex.CaptchaImg}"));
                }
                catch (VkInvalidClientException ex)
                {
                    Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.AccessDenied, "Неверный логин или пароль"));
                }
                catch (VkNeedValidationException ex)
                {
                    Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.NeedValidation, "Требуется подтверждение безопасности"));
                }
                catch (Exception ex)
                {
                    Framework.Execute.ExecuteOnUIThread(() => callback(VKErrors.UnknownError, $"Ошибка авторизации: {ex.Message}"));
                }
            });
        }

        private static void ProcessInputPage(string text, string login, string password, Action<VKErrors, string> callback)
        {
            /*
            <form method="post" action="https://login.vk.ru/?act=login&soft=1&utf8=1">
                  <input type="hidden" name="_origin" value="https://oauth.vk.ru">
                  <input type="hidden" name="ip_h" value="315d1c2d69a7223bb1" />
                  <input type="hidden" name="lg_h" value="ace7dd23d8f90d93c0" />
                  <input type="hidden" name="to" value="aHR0cHM6Ly9vYXV0aC52ay5jb20vYXV0aG9yaXplP2NsaWVudF9pZD02MjQ0ODU0JnJlZGlyZWN0X3VyaT1odHRwcyUzQSUyRiUyRm9hdXRoLnZrLmNvbSUyRmJsYW5rLmh0bWwmcmVzcG9uc2VfdHlwZT10b2tlbiZzY29wZT0xMzYyNjQ5Mjcmdj01LjkyJnN0YXRlPSZkaXNwbGF5PW1vYmlsZQ--">
                  <dl class="fi_row">
                    <dt class="fi_label">Телефон или email:</dt>
                    <dd>
                      <div class="iwrap"><input type="text" class="textfield" name="email" value="" /></div>
                    </dd>
                  </dl>
                  <dl class="fi_row">
                    <dt class="fi_label">Пароль:</dt>
                    <dd>
                      <div class="iwrap"><input type="password" class="textfield" name="pass" /></div>
                    </dd>
                  </dl>
      
                  <div class="fi_row">
                    <div class="fi_subrow">
                      <input class="button" type="submit" value="Войти" /><div class="near_btn"><a href="//oauth.vk.ru/blank.html#error=access_denied&error_reason=user_denied&error_description=User denied your request">Отмена</a></div>
                    </div>
                  </div>
                  <div class="fi_row_new">
              <div class="fi_header fi_header_light">Ещё не зарегистрированы?</div>
            </div>
            <div class="fi_row">
              <a class="button wide_button gray_button" href="https://m.vk.ru/join?api_hash=770128f2ab85662d0d" rel="noopener">Зарегистрироваться</a>
            </div>
            </form>
            */
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            Regex regexObj = new Regex("<input.+?type=\"hidden\".+?name=\"(?<name>.+?)\".+?value=\"(?<value>.+?)\"", RegexOptions.Singleline);
            MatchCollection matches = regexObj.Matches(text);
            foreach (Match m in matches)
            {
                parameters.Add(m.Groups["name"].Value, m.Groups["value"].Value);
            }
            parameters.Add("email", login);
            parameters.Add("pass", password);
            // Отправляем наш логин и пароль

            JsonWebRequest.SendHTTPRequestAsync("https://login.vk.ru/?act=login&soft=1&utf8=1", (html, result) =>
            {
                if (result)
                {
                    //<img id="captcha" alt="" src="https://m.vk.ru/captcha.php?sid=300813742407&dif=1" class="captcha_img" />
                    //<div class="service_msg service_msg_warning">Указан неверный логин или пароль.</div>
                    Regex regService = new Regex("service_msg_warning.+?>(.+?)<", RegexOptions.Singleline);
                    Match matchResults = regService.Match(html);

                    //TODO : fix "Access denied"
                    if (1==0)//(matchResults.Success)
                    {
                        callback(VKErrors.AccessDenied, matchResults.Groups[1].Value);
                        return;
                    }
                    ProcessGrand(html, login, password, callback);
                }
                else
                {
                    callback(VKErrors.NoNetwork, "Не удалось загрузить страницу oauth.vk.ru");
                }
            }, parameters);
        }


        private static void ProcessGrand(string text, string login, string password, Action<VKErrors, string> callback)
        {
            /*
            <div class="PageBlock">
              <div class="owner_panel oauth_mobile_header">
                <img src="https://vk.ru/images/dquestion_d.png" class="op_fimg" />
                <div class="op_fcont">
                  <div class="op_owner">LunaVK</div>
                  <div class="op_info">запрашивает доступ к Вашему аккаунту</div>
                </div>
              </div>
              <div class="form_item">
                <div class="fi_row">
                  <div class="oauth_access_header">
                    <span class="oauth_access_text">Приложению будут доступны:</span>
                  </div>
                  <div class="oauth_access_items">
                    <span class="oauth_access_item">сообщения</span>, <span class="oauth_access_item">информация страницы</span>, <span class="oauth_access_item">обновление статуса</span>, <span class="oauth_access_item">список друзей</span>, <span class="oauth_access_item">фотографии</span>, <span class="oauth_access_item">товары</span>, <span class="oauth_access_item">истории</span>, <span class="oauth_access_item">публикация записей</span>, <span class="oauth_access_item">аудиозаписи</span>, <span class="oauth_access_item">видео</span>, <span class="oauth_access_item">доступ в любое время</span>, <span class="oauth_access_item">заметки</span>, <span class="oauth_access_item">вики-страницы</span>, <span class="oauth_access_item">документы</span>, <span class="oauth_access_item">группы</span>, <span class="oauth_access_item">уведомления об ответах</span>, <span class="oauth_access_item">статистика</span>
                  </div>
                </div>
                <form method="post" action="https://login.vk.ru/?act=grant_access&client_id=6244854&settings=136264927&response_type=token&group_ids=&token_type=0&v=5.92&display=mobile&ip_h=315d1c2d69a7223bb1&hash=1588688729_946ca8ebe67420ea82&https=1&state=&redirect_uri=https%3A%2F%2Foauth.vk.ru%2Fblank.html">
      
                  <div class="fi_row">
                    <input class="button" type="submit" value="Разрешить" /><div class="near_btn"><a href="//oauth.vk.ru/blank.html#error=access_denied&error_reason=user_denied&error_description=User denied your request">Отмена</a></div>
                  </div>
                </form>
              </div>
              </div>
            */
            Regex regexObj = new Regex("form.+?action=\"(.+?)\"", RegexOptions.Singleline);
            Match matchResults = regexObj.Match(text);
            if (matchResults.Success)
            {
                string action = matchResults.Groups[1].Value;
                // Действие: разрешить приложению
                //<form method="post" action="https://login.vk.ru/?act=grant_access

                //ИЛИ <form method="post" action="/login?act=authcheck_code
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                if (action.Contains("grant_access"))
                {
                    JsonWebRequest.SendHTTPRequestAsync(action, (html, result) =>
                    {
                        if (result)
                        {
                            callback(VKErrors.None, html);
                        }
                        else
                        {
                            callback(VKErrors.NoNetwork, "Grand access fail.");
                        }
                    }, parameters, null, false, false);
                }
                else if (action.Contains("authcheck_code"))
                {
                    ProcessInputTextDialog(action, login, password, callback);
                }
            }
            else
            {
                callback(VKErrors.UnknownError, "Grand access unknown error.");
            }
        }


        private static void ProcessInputTextDialog(string action, string login, string password, Action<VKErrors, string> callback)
        {
            Framework.Execute.ExecuteOnUIThread(async () =>
            {
                Windows.UI.Xaml.Input.InputScope scope = new Windows.UI.Xaml.Input.InputScope();
                scope.Names.Add(new Windows.UI.Xaml.Input.InputScopeName() { NameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number });
                TextBox inputTextBox = new TextBox() { InputScope = scope };
                inputTextBox.AcceptsReturn = false;
                inputTextBox.Height = 32;
                ContentDialog dialog = new ContentDialog();
                dialog.Content = inputTextBox;
                dialog.Title = LocalizedStrings.GetString("SignUp_ConfirmationCode");
                dialog.IsSecondaryButtonEnabled = true;
                dialog.PrimaryButtonText = "Ok";
                dialog.SecondaryButtonText = LocalizedStrings.GetString("Cancel/Text");
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    string code = inputTextBox.Text;
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("code", code);

                    JsonWebRequest.SendHTTPRequestAsync("https://m.vk.ru" + action, (html, result) =>
                    {
                        if (result)
                        {
                            ProcessGrand(html, login, password, callback);
                        }
                        else
                        {
                            callback(VKErrors.NoNetwork, "No network (code state).");
                        }
                    }, parameters);
                }
                else
                    callback(VKErrors.AccessDenied, "Нужен код подтверждения.");
            });
        }
    }
}

