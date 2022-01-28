﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunkong.Core.Hoyolab;
using Xunkong.Core.Metadata;
using Xunkong.Core.Wish;

namespace Xunkong.Core.XunkongApi
{
    public class XunkongApiClient
    {


        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _options;

        private const string BaseUrl = "https://api.xunkong.cc";

        public string ApiVersion { get; init; } = "v0.1";



        public XunkongApiClient(HttpClient? httpClient = null, JsonSerializerOptions? options = null)
        {
            if (httpClient is null)
            {
                _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            }
            else
            {
                _httpClient = httpClient;
            }
            if (options is null)
            {
                _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            }
            else
            {
                _options = options;
            }
        }




        private async Task<T> CommonGetAsync<T>(string url) where T : class
        {
            var dto = await _httpClient.GetFromJsonAsync<ResponseBaseWrapper<T>>(url, _options);
            if (dto is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response body is null.");
            }
            if (dto.Code != 0)
            {
                throw new XunkongException(dto.Code, dto.Message);
            }
            // warning 不确定是否应该判断响应data为null的情况
            if (dto.Data is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response data is null.");
            }
            return dto.Data;
        }



        private async Task<T> CommonPostAsync<T>(string url, object value) where T : class
        {
            var response = await _httpClient.PostAsJsonAsync(url, value, _options);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<ResponseBaseWrapper<T>>(content, _options);
            if (dto is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response body is null.");
            }
            if (dto.Code != 0)
            {
                throw new XunkongException(dto.Code, dto.Message);
            }
            // warning 不确定是否应该判断响应data为null的情况
            if (dto.Data is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response data is null.");
            }
            return dto.Data;
        }



        private async Task<T> CommonSendAsync<T>(HttpRequestMessage request) where T : class
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<ResponseBaseWrapper<T>>(content, _options);
            if (dto is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response body is null.");
            }
            if (dto.Code != 0)
            {
                throw new XunkongException(dto.Code, dto.Message);
            }
            // warning 不确定是否应该判断响应data为null的情况
            if (dto.Data is null)
            {
                throw new XunkongException(ErrorCode.InvalidModelException, "Response data is null.");
            }
            return dto.Data;
        }




        #region Desktop Version



        public async Task<DesktopUpdateVersion> CheckDesktopUpdateAsync(ChannelType channel)
        {
            var url = $"{BaseUrl}/{ApiVersion}/desktop/checkupdate?channel={channel}";
            return await CommonGetAsync<DesktopUpdateVersion>(url);
        }



        public async Task<DesktopChangelog> GetDesktopChangelogAsync(Version version)
        {
            var url = $"{BaseUrl}/{ApiVersion}/desktop/changelog?version={version}";
            return await CommonGetAsync<DesktopChangelog>(url);
        }



        public async Task<NotificationWrapper<T>> GetNotificationsAsync<T>(ChannelType channel, Version version, int lastId = 0) where T : NotificationModelBase
        {
            var url = $"{BaseUrl}/{ApiVersion}/desktop/notifications?channel={channel}&version={version}&lastId={lastId}";
            return await CommonGetAsync<NotificationWrapper<T>>(url);
        }


        #endregion



        #region Wishlog Backup


        public async Task<WishlogBackupResult> GetWishlogBackupLastItemAsync(WishlogBackupRequestModel wishlogDto)
        {
            var url = $"{BaseUrl}/{ApiVersion}/wishlog/last";
            return await CommonPostAsync<WishlogBackupResult>(url, wishlogDto);
        }


        public async Task<WishlogBackupResult> GetWishlogBackupListAsync(WishlogBackupRequestModel wishlogDto)
        {
            var url = $"{BaseUrl}/{ApiVersion}/wishlog/get";
            return await CommonPostAsync<WishlogBackupResult>(url, wishlogDto);
        }


        public async Task<WishlogBackupResult> PutWishlogListAsync(WishlogBackupRequestModel wishlogDto)
        {
            var url = $"{BaseUrl}/{ApiVersion}/wishlog/put";
            return await CommonPostAsync<WishlogBackupResult>(url, wishlogDto);
        }


        public async Task<WishlogBackupResult> DeleteWishlogBackupAsync(WishlogBackupRequestModel wishlogDto)
        {
            var url = $"{BaseUrl}/{ApiVersion}/wishlog/delete";
            return await CommonPostAsync<WishlogBackupResult>(url, wishlogDto);
        }


        #endregion



        #region Genshin Metadata



        public async Task<IEnumerable<CharacterInfo>> GetCharacterInfosAsync()
        {
            var url = $"{BaseUrl}/{ApiVersion}/genshin/metadata/character";
            var result = await CommonGetAsync<MetadataDto<CharacterInfo>>(url);
            return result?.List!;
        }


        public async Task<IEnumerable<WeaponInfo>> GetWeaponInfosAsync()
        {
            var url = $"{BaseUrl}/{ApiVersion}/genshin/metadata/weapon";
            var result = await CommonGetAsync<MetadataDto<WeaponInfo>>(url);
            return result?.List!;
        }


        public async Task<IEnumerable<WishEventInfo>> GetWishEventInfosAsync()
        {
            var url = $"{BaseUrl}/{ApiVersion}/genshin/metadata/wishevent";
            var result = await CommonGetAsync<MetadataDto<WishEventInfo>>(url);
            return result?.List!;
        }



        #endregion





    }
}
