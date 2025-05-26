using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Net;
using System.Reflection;
using System.Data.Linq.Mapping;
using System.Text.RegularExpressions;
using System.IO;

namespace EmailDownloader
{
    public partial class Form1 : Form
    {
        //private string serverURL = "https://web.heynow.com.uy:8310";
        private string serverURL = "";
        private string begin;
        private string end;
        private bool PROD = false;
        private StringBuilder textLog = new StringBuilder();

        public Form1()
        {
            try
            {
                InitializeComponent();
                string[] args = Environment.GetCommandLineArgs();

                serverURL = ConfigurationManager.AppSettings.Get("serverURL");

                PROD = bool.Parse(ConfigurationManager.AppSettings.Get("PROD"));

                dtBegin.Value = DateTime.Now.AddHours(-1 * int.Parse(ConfigurationManager.AppSettings.Get("HorasHaciaAtras")));

                if (args.Count() > 1 && args[1] == "auto")
                {
                    LogText("--------------------------------------------------------");
                    LogText(DateTime.Now.ToString() + "  INICIANDO PROCESO AUTOMATICO");

                    groupBoxImpManual.Enabled = false;

                    LogItem("Proceso automatico: consultando sesiones a HeyNow...");

                    int horasAtras = int.Parse(ConfigurationManager.AppSettings.Get("HorasHaciaAtras"));
                    DateTime fecha = DateTime.Now.AddHours(3);
                    begin = fecha.AddHours(-1 * horasAtras).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    end = fecha.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    LogText("SOLICITANDO SESIONES A HEYNOW DESDE " + begin.ToString() + " HASTA " + end.ToString() + " (HORARIO UTC)");
                    CommitLog();
                    ProcesarSessionesYGuardarlasADB(true);
                }
                else
                {
                    groupBoxImpManual.Enabled = true;
                }
            }
            catch (Exception exc)
            {
                LogText(" <<< ERROR GRAVE: >>>");
                LogText(exc.Message);
                LogText(exc.StackTrace);
                Close();
            }
        }


        private void LogText(string log)
        {
            textLog.AppendLine(log);
        }

        private void CommitLog()
        {
            try
            {
                if (!File.Exists("log.txt"))
                {
                    FileStream fs = File.Create("log.txt");
                    fs.Close();
                }

                StreamWriter sw = new StreamWriter("log.txt", true);
                sw.WriteLine(textLog.ToString());
                textLog = new StringBuilder();
                sw.Close();
            }
            catch (Exception exc)
            { }
        }

        private void btnEjecutarImpManual_Click(object sender, EventArgs e)
        {
            try
            {
                LogText("--------------------------------------------------------");
                LogText(DateTime.Now.ToString() + "  INICIANDO PROCESO MANUAL.");

                LogItem("Proceso manual: Consultando sesiones a HeyNow...");
                begin = dtBegin.Value.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ssZ");
                end = dtEnd.Value.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ssZ");

                LogText("SOLICITANDO SESIONES A HEYNOW DESDE " + begin.ToString() + " HASTA " + end.ToString() + " (HORARIO UTC)");
                CommitLog();

                ProcesarSessionesYGuardarlasADB(false);
            }
            catch (Exception exc)
            {
                LogItem(exc.Message);
                LogText(" <<< ERROR GRAVE: >>>");
                LogText(exc.Message);
                LogText(exc.StackTrace);
            }
        }

        private async void ProcesarSessionesYGuardarlasADB(bool auto)
        {
            // LimpiarLog();
            string token = "";

            try
            {
                token = await Login();
                if (string.IsNullOrEmpty(token)) return;
            }
            catch (Exception ex)
            {
                LogItem(string.Format("    ERROR en el Login: {0}", ex.Message));
                LogText("ERROR LOGIN HEYNOW.");
                return;
            }

            List<HeyNowChatsHeaderVO> chats = null;

            try
            {
                chats = await ProcesarSessiones(token);

                if (chats == null)
                {
                    LogItem("Lista de sesiones vacia!");
                    return;
                }
            }
            catch (Exception ex)
            {
                LogItem(string.Format("    ERROR al procesar sesiones: {0}", ex.Message));
                return;
            }

            try
            {
                GuardarSesionesEnDB(chats);
                //LogProcesoADB();
            }
            catch (Exception ex)
            {
                LogItem(string.Format("    ERROR al guardar en la DB: {0}", ex.Message));
                return;
            }

            if (auto)
            {
                LogItem("Cerrando en 5 segundos...");
                timerClose.Start();
            }

            CommitLog();
        }

        private void LogProcesoADB()
        {
            try
            {
                HEYNOW_SESSION_LOG log = new HEYNOW_SESSION_LOG();

                log.LOGTEXT = GetLog();
                log.SESSION_BEGIN = DateTime.Parse(begin);
                log.SESSION_END = DateTime.Parse(end);

                DataContextManager.RefreshNew();
                DataContextManager.Context.HEYNOW_SESSION_LOGs.InsertOnSubmit(log);
                DataContextManager.Context.SubmitChanges();
            }
            catch { }
        }

        private async Task<List<HeyNowChatsHeaderVO>> ProcesarSessiones(string token)
        {
            var respuesta = await ReporteSesion(token, begin, end, 500);

            if (respuesta.code != HttpStatusCode.OK)
            {
                LogItem("Error al obtener el reporte de sesión! Error:" + respuesta.reasonPhrase);
                return null;
            }
            //scroll    ->  header
            var jsonChats = Deserialize(respuesta.result);
            var id = jsonChats.scroll.id;
            int largo = jsonChats.scroll.length;
            int total = jsonChats.scroll.total.value;

            //data  ->  body:chats
            List<HeyNowChatsHeaderVO> chats = ObtenerSessiones(jsonChats);

            LogItem(string.Format("Cantidad de sesiones parseadas:{0}", chats.Count));

            if (total > largo)
            {
                LogItem("Buscando sesiones con Scroll...");

                decimal div = (new decimal(total) / largo);
                int scrolls = (int)Math.Ceiling(div);

                for (int i = 0; i <= scrolls; i++)
                {
                    try
                    {
                        LogItem(string.Format("Scroll {0} de {1}", i, scrolls));

                        var respuestaScroll = await ScrollSesion(token, id);
                        var jsonChatsScroll = Deserialize(respuesta.result);

                        List<HeyNowChatsHeaderVO> chatsScroll = ObtenerSessiones(jsonChatsScroll);
                        chats.AddRange(chatsScroll);
                    }
                    catch (Exception ex)
                    {
                        LogItem(string.Format("    ERROR al realizar Scroll {0} de {1}: {2}", i, scrolls, ex.Message));
                    }
                }
            }

            return chats;
        }

        private void GuardarSesionesEnDB(List<HeyNowChatsHeaderVO> sesiones)
        {
            int insertados = 0;
            int errores = 0;
            foreach (var s in sesiones)
            {
                try
                {
                    DataContextManager.RefreshNew();
                    HEYNOW_WA_SESSION sesionDB = new HEYNOW_WA_SESSION();
                    LogItem("Guardando session: " + s.SessionId);
                    sesionDB.SESSIONID = s.SessionId;
                    sesionDB.AGENTNAME = s.NombreAgente;
                    sesionDB.IDAGENT = s.IdAgente;
                    sesionDB.TELEFONO = s.Telefono;
                    sesionDB.SESSION_BEGIN = s.FechaInicioSesion;
                    sesionDB.SESSION_END = s.FechaFinSesion;
                    sesionDB.IDCLIENTE = s.IdCliente;
                    sesionDB.IDMOROSO = s.IdMoroso;
                    sesionDB.NOMBRE_MOROSO = TrimNonAscii(s.NombreMoroso);
                    foreach (var c in s.ListaItems)
                    {
                        HEYNOW_WA_SESSION_MESSAGE m = new HEYNOW_WA_SESSION_MESSAGE();

                        m.SESSIONID = s.SessionId;
                        m.FECHA = c.Fecha;
                        m.IDAGENT = c.IdAgente;
                        m.INCOMING = c.Incoming;
                        m.MENSAJE = TrimNonAscii(c.Mensaje);

                        sesionDB.HEYNOW_WA_SESSION_MESSAGEs.Add(m);
                    }
                    DataContextManager.Context.HEYNOW_WA_SESSIONs.InsertOnSubmit(sesionDB);

                    if (!PROD)
                    {
                        FindLongStrings(sesionDB);
                        foreach (var c in sesionDB.HEYNOW_WA_SESSION_MESSAGEs)
                            FindLongStrings(c);
                    }

                    //try
                    //{
                    #region Insert Llamada - codigo viejo
                    //if (!string.IsNullOrEmpty(s.IDOPERADOR))
                    //{
                    //    string estadoinicial = "";
                    //    LLAMADA llamadaPrevia = DataContextManager.Context.LLAMADAs.Where(c => c.IDMOROSO == s.IdMoroso).OrderByDescending(c => c.FECHA).FirstOrDefault();
                    //    if (llamadaPrevia != null)
                    //    {
                    //        estadoinicial = llamadaPrevia.ESTADOFINAL;
                    //    }

                    //    string estadofinal = "";
                    //    CARTERA cartera = DataContextManager.Context.CARTERAs.Where(c => c.IDMOROSO == s.IdMoroso && c.IDCLIENTE == s.IdCliente).FirstOrDefault();
                    //    if (cartera != null)
                    //    {
                    //        estadofinal = cartera.ESTADO;
                    //    }

                    //    int duracion = 30;
                    //    if (s.CONTACTLASTINTERACTIONDATE.HasValue && s.FIRSTAGENTCONTACT.HasValue)
                    //    {
                    //        if (s.CONTACTLASTINTERACTIONDATE.Value > s.FIRSTAGENTCONTACT.Value)
                    //        {
                    //            duracion = (int)(s.CONTACTLASTINTERACTIONDATE.Value - s.FIRSTAGENTCONTACT.Value).TotalSeconds;
                    //        }
                    //    }

                    //    LLAMADA llamada = new LLAMADA();
                    //    llamada.IDMOROSO = s.IdMoroso;       //IDMOROSO - el idmoroso correspondiente OK
                    //    llamada.IDCLIENTE = s.IdCliente;      //IDCLIENTE - el idcliente correspondiente OK
                    //    llamada.IDOPERADOR = s.IDOPERADOR;              //IDOPERADOR - podemos tomar este dato de la API de Hey? IDOP
                    //    llamada.FECHA = s.FIRSTAGENTCONTACT.Value;                   //FECHA - fecha de la sesión fecha = panelfirstcontact, duracion es la resta.
                    //    llamada.HORA = s.FIRSTAGENTCONTACT.Value.ToString("HH:mm:ss");                    //HORA - contactLastInteractionDate? OK.

                    //    //Codigo 20220309 VER ACÁ
                    //    //llamada.TIPO = "WHATSAPP";                    //TIPO - Whatsapp OK NOTA EL CAMPO ES  CHAR(10)
                    //    llamada.TIPO = s.derivadoAOperador ? "WTS_CHAT" : "WTS_BOT";   // "WHATSAPP_CHAT":"WHATSAPP_BOT";   //NO ENTRA EL LARGO de la descripcion
                    //                                                                   //\Codigo 20220309

                    //    llamada.DURACION = duracion;                //DURACION - diferencia entre firstAgentContact y contactLastInteractionDate. En caso de que la diferencia sea menor a cero (cuando el moroso nunca respondió) insertamos la llamada como una gestión de 30 segundos OK
                    //    llamada.ESTADOINICIAL = estadoinicial;           //ESTADOINICIAL - el que tiene en CARTERA blanco
                    //    llamada.ESTADOFINAL = estadofinal;             //ESTADOFINAL - el que tiene en CARTERA blanco
                    //    llamada.CONTACTO = "WHATSAPP";                //CONTACTO - Whatsapp OK.
                    //    llamada.DIALED = s.Telefono;                  //DIALED - se puede sacar de la API de Hey el dato del teléfono del cual está escribiendo el moroso? OK
                    //    llamada.RESULTADO = s.SessionId;               //RESULTADO - Sesión de Whatsapp OK
                    //    llamada.COMENTARIO = "Se registra sesión llevada a cabo en Whatsapp";              //COMENTARIO - Se registra sesión llevada a cabo en Whatsapp OK
                    //    DataContextManager.Context.LLAMADAs.InsertOnSubmit(llamada);

                    //    LogItem(string.Format("    LLAMADA INSERTADA OK: {0}", s.SessionId));
                    //    LogText(string.Format("LLAMADA INSERTADA OK: {0}  MOROSO:{1}  CLIENTE:{2}", s.SessionId, s.IdMoroso, s.IdCliente));
                    //}
                    #endregion

                    ACT_WHATSAPP_GESTION_DE_CHAT gestChat = new ACT_WHATSAPP_GESTION_DE_CHAT();
                    //nuevo
                    gestChat.IdCliente = s.IdCliente;
                    gestChat.IdMoroso = s.IdMoroso;
                    //\nuevo

                    gestChat.FechaHora = DateTime.Now;
                    gestChat.Fuente = "HEYNOW";
                    gestChat.Telefono = s.Telefono;
                    gestChat.IdOparador = s.derivadoAOperador ? s.IDOPERADOR : "VIRTUAL";
                    gestChat.ResolucionId = 2;    //2 =   "Gest.HN"
                    gestChat.CategorizacionId = 2; //2 = "Neutro"
                    gestChat.ContactoId = 4; //4    Desconocido
                    DataContextManager.Context.ACT_WHATSAPP_GESTION_DE_CHATs.InsertOnSubmit(gestChat);

                    //}
                    //catch (Exception ex)
                    //{
                    //LogItem(string.Format("    INSERT ACT_WHATSAPP_GESTION_DE_CHAT CON ERROR: {0}", s.SessionId));
                    //LogText(string.Format("INSERT ACT_WHATSAPP_GESTION_DE_CHAT CON ERROR: {0}  MOROSO:{1}  CLIENTE:{2}   ERROR: {3}", s.SessionId, s.IdMoroso, s.IdCliente, ex.Message));


                    //LogItem(string.Format("    INSERT LLAMADA CON ERROR: {0}", s.SessionId));
                    //LogText(string.Format("INSERT LLAMADA CON ERROR: {0}  MOROSO:{1}  CLIENTE:{2}   ERROR: {3}", s.SessionId, s.IdMoroso, s.IdCliente, ex.Message));
                    //}
                    if (PROD)
                    {
                        DataContextManager.Context.SubmitChanges();
                    }

                    insertados++;
                    LogItem("    OK");
                    LogText(string.Format("SESION INGRESADA OK: {0}  MOROSO:{1}  CLIENTE:{2}", s.SessionId, s.IdMoroso, s.IdCliente));
                }
                catch (Exception exc)
                {
                    if (exc.Message.Contains("PK_HEYNOW_WA_SESSION"))
                        LogItem(string.Format("    La session {0} ya se encuentra en la base de datos.", s.SessionId));
                    else
                    {
                        LogItem(string.Format("    ERROR guardando en base de datos la sesión id:{0}, Error: {1}. IdCliente: {2}({3}), IdMoroso: {4}({5})",
                                                s.SessionId, exc.Message, s.IdCliente, s.IdCliente.Length, s.IdMoroso, s.IdMoroso.Length));

                        LogText(string.Format("    ERROR guardando en base de datos la sesión id:{0}, Error: {1}. IdCliente: {2}({3}), IdMoroso: {4}({5})",
                                                s.SessionId, exc.Message, s.IdCliente, s.IdCliente.Length, s.IdMoroso, s.IdMoroso.Length));
                    }
                    errores++;
                }
            }

            LogItem(string.Format("Sesiones insertadas: {0}, sesiones con error: {1}", insertados, errores));
        }

        public void FindLongStrings(object testObject)
        {
            foreach (PropertyInfo propInfo in testObject.GetType().GetProperties())
            {
                foreach (ColumnAttribute attribute in propInfo.GetCustomAttributes(typeof(ColumnAttribute), true))
                {
                    if (attribute.DbType.ToLower().Contains("varchar"))
                    {
                        string dbType = attribute.DbType.ToLower();
                        int numberStartIndex = dbType.IndexOf("varchar(") + 8;
                        int numberEndIndex = dbType.IndexOf(")", numberStartIndex);
                        string lengthString = dbType.Substring(numberStartIndex, (numberEndIndex - numberStartIndex));
                        int maxLength = 0;
                        int.TryParse(lengthString, out maxLength);

                        string currentValue = (string)propInfo.GetValue(testObject, null);

                        if (!string.IsNullOrEmpty(currentValue) && maxLength != 0 && currentValue.Length > maxLength)
                            LogItem(testObject.GetType().Name + "." + propInfo.Name + " " + currentValue + " Max: " + maxLength);
                    }
                }
            }
        }

        public string TrimNonAscii(string value)
        {
            string pattern = "[^ -~]+";
            Regex reg_exp = new Regex(pattern);
            return reg_exp.Replace(value, "");
        }

        #region Parser JSON
        private List<HeyNowChatsHeaderVO> ObtenerSessiones(dynamic jsonChats)
        {
            List<HeyNowChatsHeaderVO> result = new List<HeyNowChatsHeaderVO>();

            var data = jsonChats.data;

            LogItem(string.Format("Cantidad de sesiones en JSON:{0}", data.Count));

            for (int i = 0; i < data.Count; i++)
            {
                try
                {
                    var dataItem = data[i];

                    HeyNowChatsHeaderVO header = ObtenerHeader(dataItem);

                    if (header == null) continue;

                    LogItem("Procesando sesion: " + header.SessionId);
                    LogItem(string.Format("    T: {0}  //  M: {1}  //  C: {2}  //  H: {3}   //   A: {4}", header.Telefono, header.IdMoroso, header.IdCliente, header.FechaFinSesion.ToString(), header.NombreAgente));

                    var source = dataItem._source;

                    var chats = source.chat;
                    for (int j = 0; j < chats.Count; j++)
                    {
                        var chat = chats[j];
                        HeyNowChatsItemVO chatVO = ObtenerChat(chat, header.SessionId);

                        if (chatVO == null) continue;

                        header.ListaItems.Add(chatVO);
                    }
                    result.Add(header);
                }
                catch (Exception exx)
                {
                    LogItem(string.Format("Error al parsear una session: Error: {0}", exx.Message));
                }
            }

            return result;
        }

        private HeyNowChatsItemVO ObtenerChat(dynamic chat, string sessionId)
        {

            try
            {
                var chatId = chat._id;
                var mensaje = chat.message;

                if (string.IsNullOrEmpty(mensaje))
                    return null;

                var incoming = chat.incoming;
                var chatFecha = chat.date;
                DateTime? chatFechaDate = null;
                if (chatFecha != null)
                    chatFechaDate = DateTime.Parse(chatFecha);
                var IdAgent = chat.idAgent;
                string idAgentS = IdAgent != null ? IdAgent.ToString() : "";

                //var firstAgentContact = chat.FirstAgentContact;

                HeyNowChatsItemVO result = new HeyNowChatsItemVO
                {
                    Fecha = chatFechaDate,
                    Id = sessionId,
                    IdAgente = idAgentS,
                    Incoming = incoming,
                    Mensaje = mensaje
                };

                return result;
            }
            catch (Exception ex)
            {
                LogItem(string.Format("Error al parsear un chat de sessionId {0}: Error: {1} ", sessionId, ex.Message));
                return null;
            }
        }

        //parametro: dataItem = elementos "_source"
        private HeyNowChatsHeaderVO ObtenerHeader(dynamic dataItem)
        {
            //Cambio 20240221
            //var id = dataItem._id;
            var source = dataItem._source;
            var id = source._id;
            var beginSession = source.beginSession;
            DateTime beginSessionDate = DateTime.Parse(beginSession);
            var endSession = source.endSession;
            DateTime endSessionDate = DateTime.Parse(endSession);
            var nombre = source.contact.first_name;
            var telefonos = source.contact.phones;
            var telefono = "";
            if (telefonos != null && telefonos.Count > 0)
                telefono = telefonos[0];
            else
            {
                telefono = "WEBCHAT";
            }


            var documento = source.queryData.documento;
            if (documento == null)
            {
                if (ckVerSinDatos.Checked)
                    LogItem(string.Format("    SessionId: {0} no tiene documento (idMoroso)", id));
                return null;
            }

            var idMoroso = documento;
            var empresa = source.queryData.empresa;
            if (empresa == null)
            {
                if (ckVerSinDatos.Checked)
                    LogItem(string.Format("    SessionId: {0} no tiene empresa (idCliente)", id));
                return null;
            }
            var idCliente = empresa;
            var idAgente = "";
            var nombreAgente = "";

            var outcoming = source.messages.outcoming;
            try
            {
                if (outcoming != null && outcoming.Count > 0)
                {
                    foreach (var o in outcoming)
                    {
                        if (o.agents != null && o.agents.Count > 0)
                        {
                            foreach (var agent in o.agents)
                            {
                                if (agent.idAgent != null && string.IsNullOrWhiteSpace(idAgente) && string.IsNullOrEmpty(idAgente))
                                {
                                    idAgente = agent.idAgent.ToString();
                                }

                                if (agent.names != null && string.IsNullOrWhiteSpace(nombreAgente) && string.IsNullOrEmpty(nombreAgente))
                                {
                                    nombreAgente = agent.names;
                                }

                                if (agent.lastNames != null)
                                {
                                    if (string.IsNullOrWhiteSpace(nombreAgente) && string.IsNullOrEmpty(nombreAgente))
                                    {
                                        nombreAgente = agent.lastNames;
                                    }
                                    else
                                    {
                                        nombreAgente += " " + agent.lastNames;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }


            string idoperador = "";

            try
            {
                if (outcoming != null && outcoming.Count > 0)
                {
                    for (int j = 0; j <= outcoming.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(idoperador))
                            break;

                        if (outcoming[j] != null && outcoming[j].agents != null)
                        {
                            for (int k = 0; k <= outcoming[j].agents.Count; k++)
                            {
                                idoperador = outcoming[j].agents[k].login;
                                if (!string.IsNullOrEmpty(idoperador) && !string.IsNullOrWhiteSpace(idoperador))
                                    break;
                            }
                        }
                    }
                }
            }
            catch { }

            DateTime? firstagentcontact = (DateTime?)null;
            try
            {
                if (source.firstAgentContact != null)
                {
                    firstagentcontact = (DateTime?)Convert.ToDateTime(source.firstAgentContact);
                }
            }
            catch { }

            DateTime? contactlastinteractiondate = (DateTime?)null;
            try
            {
                if (source.contactLastInteractionDate != null)
                {
                    contactlastinteractiondate = (DateTime?)Convert.ToDateTime(source.contactLastInteractionDate);
                }
            }
            catch { }

            //var sourceMessages = source.messages.ToList();
            //var derivadoAOperador = false;
            //foreach (var message in source.messages)
            //{
            //    if(message.startPannel != null)
            //    {
            //        derivadoAOperador = true;
            //        break;
            //    }
            //}


            HeyNowChatsHeaderVO header = new HeyNowChatsHeaderVO
            {
                SessionId = id,
                FechaInicioSesion = beginSessionDate,
                FechaFinSesion = endSessionDate,
                IdCliente = idCliente,
                IdMoroso = idMoroso,
                NombreAgente = nombreAgente,
                IdAgente = idAgente,
                NombreMoroso = nombre,
                Telefono = telefono,
                IDOPERADOR = idoperador,
                FIRSTAGENTCONTACT = firstagentcontact,
                CONTACTLASTINTERACTIONDATE = contactlastinteractiondate,
                derivadoAOperador = source.startPannelDate != null//derivadoAOperador
            };

            return header;
        }
        #endregion Parser JSON

        private dynamic Deserialize(string json)
        {
            var serialize = new JavaScriptSerializer();
            serialize.RegisterConverters(new[] { new DynamicJsonConverter() });
            return serialize.Deserialize(json, typeof(object));
        }

        #region llamados WS
        private async Task<string> Login()
        {
            string user = ConfigurationManager.AppSettings.Get("ApiUser");
            string pass = ConfigurationManager.AppSettings.Get("ApiPassword");

            var resLogin = await LoginApiCall(user, pass);

            if (resLogin.code != HttpStatusCode.OK)
            {
                LogItem("Error de login! Error: " + resLogin.errorMessage + " " + resLogin.reasonPhrase);
                return null;
            }

            dynamic jsonLogin = Deserialize(resLogin.result);

            return jsonLogin.token;
        }

        private async Task<DownloadPageAsyncResult> LoginApiCall(string user, string pass)
        {
            string postData = "name=" + user + "&password=" + pass;
            var service = new HTTPRequestService();

            object param = new { name = user, password = pass };
            var serialize = new JavaScriptSerializer();

            string json = serialize.Serialize(param);

            var res = await service.SendRequestAsync(serverURL + "/api/login", json, null, true);

            var respuesta = await service.DownloadPageAsync(res);

            return respuesta;
        }

        private async Task<DownloadPageAsyncResult> ReporteSesion(string token, string begin, string end, int pageSize)
        {
            string postData2 = "begin=" + begin + "&end=" + end + "&pageSize=" + pageSize;
            var service = new HTTPRequestService();

            var res = await service.SendRequestAsync(serverURL + "/api/report/session?" + postData2, "", token, false);

            var respuesta = await service.DownloadPageAsync(res);

            return respuesta;
        }

        private async Task<DownloadPageAsyncResult> ScrollSesion(string token, string scrollId)
        {
            string postData = "scrollId=" + scrollId;
            var service = new HTTPRequestService();

            var res = await service.SendRequestAsync(serverURL + "/api/report/session?" + postData, "", token, false);

            var respuesta = await service.DownloadPageAsync(res);

            return respuesta;
        }
        #endregion llamados WS

        private void LogItem(string item)
        {
            while (listBox1.Items.Count > 1000)
                listBox1.Items.RemoveAt(0);

            listBox1.Items.Add(DateTime.Now.ToShortTimeString() + " - " + item);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void LimpiarLog()
        {
            listBox1.Items.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.DoEvents();
        }

        private void btnClip_Click(object sender, EventArgs e)
        {
            string log = GetLog();
            if (log != null)
                Clipboard.SetText(log);
        }

        private string GetLog()
        {
            StringBuilder log = new StringBuilder();
            foreach (var s in listBox1.Items)
            {
                log.AppendLine(s.ToString());
            }
            return log.ToString();
        }

        private void timerClose_Tick(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
