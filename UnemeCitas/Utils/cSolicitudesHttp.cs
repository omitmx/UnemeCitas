using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using UnemeCitas.Models.Api;
using System.Net.Http.Handlers;

namespace UnemeCitas.Utils
{
    public enum MethodoHttp
    {
        GET = 0,
        POST = 1,
        PUT = 2,
        DELETE = 3,
        PROPFIND = 4,
        MKCOL = 5
    }
    public enum TipoData
    {
        JSON = 0,
        XML = 1,
        NONE = 2
    }
    public delegate void OnFileProgress(string filename, float progress);
    public delegate void OnFileComplete(string filename, string url);
    public class cSolicitudesHttp<TModel>
    {
        public event OnFileProgress? FileProgress;
        //public event OnFileComplete? FileComplete;
        public string EstatusPeticion { get; set; }
        TModel data;
        MethodoHttp metodo = MethodoHttp.GET;
        TipoData tdata;
        List<vmHeaderHttp> lstHeader = new();
        string Token;
        public cSolicitudesHttp(TModel data, MethodoHttp metodo, TipoData tdata)
        {
            this.EstatusPeticion = "";
            this.data = data;
            this.metodo = metodo;
            this.tdata = tdata;
            this.Token = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
        }
        public cSolicitudesHttp(TModel data, MethodoHttp metodo, TipoData tdata, string Token)
        {
            this.EstatusPeticion = "";
            this.data = data;
            this.metodo = metodo;
            this.tdata = tdata;
            this.Token = Token;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
        }
        public async Task<vmRespuesta> SolicitudHttpMultipartAsync(string url, List<vmParamApi> paramData)
        {
            vmRespuesta oRespuesta = new();
            try
            {
                var urlParam = "";

                if (data != null && metodo == MethodoHttp.GET)
                {
                    List<vmParamApi> param = (data as IEnumerable<vmParamApi>)!.Cast<vmParamApi>().ToList();
                    foreach (vmParamApi ele in param)
                    {
                        if (urlParam == "")
                        {
                            urlParam += $"?{ele.NombreCampo}={ele.ValorCampo}";
                        }
                        else
                        {
                            urlParam += $"&{ele.NombreCampo}={ele.ValorCampo}";
                        }

                    }
                }
                //define uri de api
                urlParam = url + urlParam;

                using (var oReq = new HttpClient())
                {

                    //definir encabezados
                    if (lstHeader != null)
                    {
                        lstHeader.ForEach(x => oReq.DefaultRequestHeaders.Add(x.Campo??"", x.Valor));
                    }
                    if (tdata == TipoData.XML)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    else
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (Token != "")
                        oReq.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);


                    // HttpResponseMessage oResHttp;
                    if (metodo != MethodoHttp.GET)
                    {

                        string boundary = Guid.NewGuid().ToString();

                        using (var handler = new ProgressMessageHandler())
                        using (var client = HttpClientFactory.Create(handler))
                        using (var formData = new MultipartFormDataContent())
                        {
                            client.Timeout = new TimeSpan(1, 0, 0); // 1 hour should be enough probably

                            foreach (vmParamApi ele in paramData)
                            {
                                byte[] docto = new byte[1];
                                if (ele.ValorCampo!.GetType() == docto.GetType())
                                {

                                    Stream fileStream = new MemoryStream((byte[])ele!.ValorCampo);

                                    HttpContent fileStreamContent = new StreamContent(fileStream);
                                    string nombreArchivo = $"archivo_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                                    formData.Add(fileStreamContent, ele.NombreCampo ?? "", nombreArchivo);

                                    handler.HttpSendProgress += (s, e) =>
                                    {
                                        float prog = (float)e.BytesTransferred / (float)fileStream.Length;
                                        prog = prog > 1 ? 1 : prog;

                                        if (FileProgress != null)
                                            FileProgress(nombreArchivo, prog);
                                    };
                                }
                                else
                                {
                                    HttpContent httpContent = new StringContent(ele.ValorCampo.ToString() ?? "", Encoding.UTF8);
                                    formData.Add(httpContent, ele.NombreCampo ?? "");
                                }
                            }







                            var oResHttp = client.PostAsync(url, formData);

                            var response = oResHttp.Result;
                            EstatusPeticion = response.IsSuccessStatusCode.ToString();
                            EstatusPeticion = response.StatusCode.ToString();

                            //if (!response.IsSuccessStatusCode)
                            //{
                            //    return null;
                            //}
                            //return response.Content.ReadAsStreamAsync().Result;

                            if (EstatusPeticion == "OK" || EstatusPeticion == "207" || EstatusPeticion.ToUpper() == "CREATED" || EstatusPeticion == "204" || EstatusPeticion == "201")
                            {

                                var strData = await response.Content.ReadAsStringAsync();
                                if (strData != "")
                                {
                                    oRespuesta.Resultado = 1;
                                    if (strData == "[]")
                                    {
                                        oRespuesta.Data = null;
                                        oRespuesta.Msg = "Sin registros...!";
                                    }
                                    else
                                        oRespuesta.Data = strData;
                                }
                                else
                                {
                                    if (oRespuesta.Msg == "")
                                        oRespuesta.Msg = "Verifique informacion ó Favor de intentar mas tarde...!";
                                    else
                                        oRespuesta.Msg = oRespuesta.Msg;
                                }
                            }



                        }

                        // oResHttp = await oReq.PostAsync(urlParam, httpContent);



                    }

                }

            }
            catch (Exception ex)
            {
                oRespuesta.Msg = $"Error:{ex.Message}";
            }

            return oRespuesta;
        }

        public async Task<vmRespuesta> SolicitudHttpAsync(string url)
        {
            vmRespuesta oRespuesta = new();

            try
            {
                var urlParam = "";

                if (data != null && metodo == MethodoHttp.GET)
                {
                    List<vmParamApi> param = (data as IEnumerable<vmParamApi>)!.Cast<vmParamApi>().ToList();
                    foreach (vmParamApi ele in param)
                    {
                        if (urlParam == "")
                        {
                            urlParam += $"?{ele.NombreCampo}={ele.ValorCampo}";
                        }
                        else
                        {
                            urlParam += $"&{ele.NombreCampo}={ele.ValorCampo}";
                        }

                    }
                }


                urlParam = url + urlParam;

                using (var oReq = new HttpClient())
                {

                    //definir encabezados
                    if (lstHeader != null)
                    {
                        lstHeader.ForEach(x => oReq.DefaultRequestHeaders.Add(x.Campo??"", x.Valor));
                    }
                    if (tdata == TipoData.XML)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    else if (tdata == TipoData.JSON)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (Token != "")
                        oReq.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);


                    HttpResponseMessage oResHttp;
                    if (metodo == MethodoHttp.GET)
                    {
                        oResHttp = await oReq.GetAsync(urlParam);
                        EstatusPeticion = oResHttp.StatusCode.ToString();
                    }
                    else
                    {
                        //var oJson = cFuncionesJson<TModel>.SerializeJson(data);
                        //HttpContent httpContent = new StringContent(oJson);
                        //HttpContent httpContent = new StringContent(oJson, Encoding.UTF8);
                        //oResHttp = await oReq.PostAsync(urlParam, httpContent);

                        //if (tdata == TipoData.XML)
                        //    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                        //else
                        //    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        oResHttp = await oReq.PostAsJsonAsync(urlParam, data);
                        EstatusPeticion = oResHttp.StatusCode.ToString();


                    }
                    if (EstatusPeticion == "OK" || EstatusPeticion == "207" || EstatusPeticion.ToUpper() == "CREATED" || EstatusPeticion == "204" || EstatusPeticion == "201")
                    {

                        var strData = await oResHttp.Content.ReadAsStringAsync();
                        if (strData != "")
                        {
                            oRespuesta.Resultado = 1;
                            if (strData == "[]")
                            {
                                oRespuesta.Data = null;
                                oRespuesta.Msg = "Sin registros...!";
                            }
                            else
                                oRespuesta.Data = strData;
                        }
                        else
                        {
                            if (oRespuesta.Msg == "")
                                oRespuesta.Msg = "Verifique informacion ó Favor de intentar mas tarde...!";
                            else
                                oRespuesta.Msg = oRespuesta.Msg;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                oRespuesta.Msg = $"Error:{ex.Message}";
            }

            return oRespuesta;
        }
        public async Task<vmRespuesta> SolicitudHttpUrlEncodedAsync(string url)
        {
            vmRespuesta oRespuesta = new();

            try
            {
                var urlParam = "";

                if (data != null && metodo == MethodoHttp.GET)
                {
                    List<vmParamApi> param = (data as IEnumerable<vmParamApi>)!.Cast<vmParamApi>().ToList();
                    foreach (vmParamApi ele in param)
                    {
                        if (urlParam == "")
                        {
                            urlParam += $"?{ele.NombreCampo}={ele.ValorCampo}";
                        }
                        else
                        {
                            urlParam += $"&{ele.NombreCampo}={ele.ValorCampo}";
                        }

                    }
                }


                urlParam = url + urlParam;

                using (var oReq = new HttpClient())
                {

                    //definir encabezados
                    if (lstHeader != null)
                    {
                        lstHeader.ForEach(x => oReq.DefaultRequestHeaders.Add(x.Campo??"", x.Valor));
                    }
                    if (tdata == TipoData.XML)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    else
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (Token != "")
                        oReq.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);


                    HttpResponseMessage oResHttp;
                    if (metodo == MethodoHttp.GET)
                    {
                        //oRespuesta = await oReq.GetFromJsonAsync<vmRespuesta>(urlParam,new JsonSerializerOptions() {PropertyNameCaseInsensitive=true});
                        oResHttp = await oReq.GetAsync(urlParam);
                        EstatusPeticion = oResHttp.StatusCode.ToString();
                    }
                    else
                    {
                        List<KeyValuePair<string, string>> param = (data as IEnumerable<KeyValuePair<string, string>>)!.Cast<KeyValuePair<string, string>>().ToList();
                        HttpContent httpContent = new FormUrlEncodedContent(param);
                        oResHttp = await oReq.PostAsync(urlParam, httpContent);
                        EstatusPeticion = oResHttp.StatusCode.ToString();


                    }
                    if (EstatusPeticion == "OK" || EstatusPeticion == "207" || EstatusPeticion.ToUpper() == "CREATED" || EstatusPeticion == "204" || EstatusPeticion == "201")
                    {

                        var strData = await oResHttp.Content.ReadAsStringAsync();
                        if (strData != "")
                        {
                            oRespuesta.Resultado = 1;
                            if (strData == "[]")
                            {
                                oRespuesta.Data = null;
                                oRespuesta.Msg = "Sin registros...!";
                            }
                            else
                                oRespuesta.Data = strData;
                        }
                        else
                        {
                            if (oRespuesta.Msg == "")
                                oRespuesta.Msg = "Verifique informacion ó Favor de intentar mas tarde...!";
                            else
                                oRespuesta.Msg = oRespuesta.Msg;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                oRespuesta.Msg = $"Error:{ex.Message}";
            }

            return oRespuesta;
        }


        public async Task<vmRespuesta> SolicitudHttpBinaryAsync(string url, byte[] docto)
        {
            vmRespuesta oRespuesta = new();

            try
            {
                var urlParam = "";

                if (data != null && metodo == MethodoHttp.GET)
                {
                    List<vmParamApi> param = (data as IEnumerable<vmParamApi>)!.Cast<vmParamApi>().ToList();
                    foreach (vmParamApi ele in param)
                    {
                        if (urlParam == "")
                        {
                            urlParam += $"?{ele.NombreCampo}={ele.ValorCampo}";
                        }
                        else
                        {
                            urlParam += $"&{ele.NombreCampo}={ele.ValorCampo}";
                        }

                    }
                }


                urlParam = url + urlParam;

                using (var oReq = new HttpClient())
                {

                    //definir encabezados

                    if (tdata == TipoData.XML)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    else if (tdata == TipoData.JSON)
                        oReq.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (Token != "")
                        oReq.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);




                    HttpResponseMessage oResHttp;
                    if (metodo == MethodoHttp.GET)
                    {
                        //oRespuesta = await oReq.GetFromJsonAsync<vmRespuesta>(urlParam,new JsonSerializerOptions() {PropertyNameCaseInsensitive=true});
                        oResHttp = await oReq.GetAsync(urlParam);
                        EstatusPeticion = oResHttp.StatusCode.ToString();
                    }
                    else
                    {
                        var strData = "";
                        HttpContent httpContent = new ByteArrayContent(docto);
                        if (lstHeader != null)
                        {
                            lstHeader.ForEach(x => httpContent.Headers.Add(x.Campo??"", x.Valor));
                        }
                        if (metodo == MethodoHttp.POST)
                        {

                            oResHttp = await oReq.PostAsync(urlParam, httpContent);
                            EstatusPeticion = oResHttp.StatusCode.ToString();
                            if (EstatusPeticion == "OK" || EstatusPeticion == "207" || EstatusPeticion.ToUpper() == "CREATED" || EstatusPeticion == "204" || EstatusPeticion == "201" || EstatusPeticion == "202" || EstatusPeticion.ToUpper() == "ACCEPTED")
                            {

                                strData = await oResHttp.Content.ReadAsStringAsync();

                            }
                        }
                        else if (metodo == MethodoHttp.PUT)
                        {

                            oResHttp = await oReq.PutAsync(urlParam, httpContent);

                            EstatusPeticion = oResHttp.StatusCode.ToString();
                            if (EstatusPeticion == "OK" || EstatusPeticion == "207" || EstatusPeticion.ToUpper() == "CREATED" || EstatusPeticion == "204" || EstatusPeticion == "201" || EstatusPeticion == "202" || EstatusPeticion.ToUpper() == "ACCEPTED")
                            {

                                strData = await oResHttp.Content.ReadAsStringAsync();

                            }

                        }
                        if (strData != "")
                        {
                            oRespuesta.Resultado = 1;
                            if (strData == "[]")
                            {
                                oRespuesta.Data = null;
                                oRespuesta.Msg = "Sin registros...!";
                            }
                            else
                                oRespuesta.Data = strData;
                        }
                        else
                        {
                            if (oRespuesta.Msg == "")
                                oRespuesta.Msg = "Verifique informacion ó Favor de intentar mas tarde...!";
                            else
                                oRespuesta.Msg = oRespuesta.Msg;
                        }

                    }

                }

            }
            catch (Exception ex)
            {
                oRespuesta.Msg = $"Error:{ex.Message}";
            }

            return oRespuesta;
        }


        #region HELPERS

        public void AddHeader(string name, string value)
        {

            if (lstHeader == null)
                lstHeader = new List<vmHeaderHttp>();
            lstHeader.Add(new vmHeaderHttp() { Campo = name, Valor = value });

        }
        private string GetValorMetodo(MethodoHttp metodo)
        {
            string resultado = "";
            if (metodo == MethodoHttp.GET)
                resultado = "GET";
            else if (metodo == MethodoHttp.POST)
                resultado = "POST";
            else if (metodo == MethodoHttp.PUT)
                resultado = "PUT";
            else if (metodo == MethodoHttp.DELETE)
                resultado = "DELETE";
            else if (metodo == MethodoHttp.PROPFIND)
                resultado = "PROPFIND";
            else if (metodo == MethodoHttp.MKCOL)
                resultado = "MKCOL";


            return resultado;
        }
        #endregion

    }
}
