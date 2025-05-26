using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using WEBAPP.DataAccess;
using WEBAPP.Helpers;
using WEBAPP.VO;

namespace WEBAPP.Applications.DYNAMIC.AMEX.AMEX_ACORN
{
    public partial class AmexAcornIngresarArchivosJson : WebAppPage
    {
        public bool _test = false;
        public int countTest = 0;
        public string placementSeccion = "";
        public string updateSeccion = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            //lbErrorPlacement.Text = "";
            //divRowLbErrorPlacement.Visible = false;

            MostrardivLbError(false,
                                "",
                                divRowLbErrorPlacement,
                                divLbErrorPlacement,
                                lbErrorPlacement,
                                false);
            MostrardivLbError(false,
                                "",
                                divRowLbErrorRecall,
                                divLbErrorRecall,
                                lbErrorRecall,
                                false);
            MostrardivLbError(false,
                                "",
                                divRowLbErrorUpdate,
                                divLbErrorUpdate,
                                lbErrorUpdate,
                                false);
            MostrardivLbError(false,
                                "",
                                divRowLbErrorActionsHistory,
                                divLbErrorActionsHistory,
                                lbErrorActionsHistory,
                                false);

            if (!string.IsNullOrEmpty(Request.QueryString["test"]))
            {
                _test = true;
            }


        }

        #region Procesar Archivo Placement
        protected void btnProcesarArchivoPlacement_Click(object sender, EventArgs e)
        {
            bool huboError = false;
            string msj = "La operación se realizó con éxito";
            string relativePath = VirtualPathUtility.ToAbsolute("~/File/AMEX_ACORN/ejemplo.json");
            string path = Server.MapPath(relativePath);

            try
            {
                if (!fuArchivosAcornPlacement.FileName.Contains("PLCMNT"))
                    throw new Exception("El archivo ingresado no es del tipo Placement");

                if (fuArchivosAcornPlacement.HasFile)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    fuArchivosAcornPlacement.SaveAs(path);

                    System.IO.StreamReader file = new System.IO.StreamReader(path);
                    var json = file.ReadToEnd();
                    file.Close();

                    var serialize = new JavaScriptSerializer();
                    serialize.RegisterConverters(new[] { new DynamicJsonConverter() });

                    dynamic obj = serialize.Deserialize(json, typeof(object));
                    ResultVO resultVO = CargarObjetoPlacement(obj.caseInfoList, DateTime.Now);

                    if (resultVO.isError)
                    {
                        string mesj = "Ocurio un error al procesar el archivo Placement. " + resultVO.Mensaje;
                        throw new Exception(mesj);
                    }
                }
                else
                {
                    throw new Exception("No se cargaron archivos");
                }
            }
            catch (Exception ex)
            {
                huboError = true;
                msj = ex.Message;
            }
            MostrardivLbError(huboError, msj, divRowLbErrorPlacement, divLbErrorPlacement, lbErrorPlacement);

            if (!huboError)
            {
                CrearArchivoDeRecepcion(fuArchivosAcornPlacement.FileName, path, "PLCMNT", "ACORNMOPLCMNT");
            }
        }
        protected ResultVO CargarObjetoPlacement(dynamic obj, DateTime dtNow)
        {
            int vueltaGral, vueltaMorosoPhones;
            vueltaGral = vueltaMorosoPhones = 0;
            bool esErrorEnMoroso, esErrorEnCartera, esErrorEnProducto, esErrorEnSkiptrace, esErrorEnAmexPagosMinimo, esErrorAmexInfoAdicA, esErrorMorosolast;
            esErrorEnMoroso = esErrorEnCartera = esErrorEnProducto = esErrorEnSkiptrace = esErrorEnAmexPagosMinimo = esErrorAmexInfoAdicA = esErrorMorosolast = false;
            string moroso = "";
            ResultVO resultVO = new ResultVO()
            {
                isError = false,
                Mensaje = ""
            };
            try
            {
                //Para pasar a produccion  sacar el "_AMEX_PRUEBA"
                //y que quede MOROSO, PRODUCTO, CARTERA, SKIPTRACE, AMEX_PAGOS_MINIMOS, AMEX_INFO_ADIC_A
                List<MOROSO> listMoroso = new List<MOROSO>();
                List<WEBAPP.DataAccess.CARTERA> listCartera = new List<WEBAPP.DataAccess.CARTERA>();
                List<PRODUCTO> listProducto = new List<PRODUCTO>();
                List<WEBAPP.DataAccess.SKIPTRACE> listSkiptrace = new List<WEBAPP.DataAccess.SKIPTRACE>();
                List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO> listAmexPagosMinimo = new List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO>();
                List<AMEX_INFO_ADIC_A> listAmexInfoAdicA = new List<AMEX_INFO_ADIC_A>();
                List<MOROSOLAST> listMorosolast = new List<MOROSOLAST>();

                //List<AMEX_LOG_DE_PAGO> listAmexLogDePago = new List<AMEX_LOG_DE_PAGO>();



                for (int i = 0; i < obj.Count; i++)
                {
                    vueltaMorosoPhones = 0;
                    esErrorEnMoroso = esErrorEnCartera = esErrorEnProducto = esErrorEnSkiptrace = esErrorAmexInfoAdicA = esErrorEnAmexPagosMinimo = true;
                    ++vueltaGral;
                    moroso = obj[i].accountInfoList[0].accountNumber;


                    placementSeccion = "demographics";
                    listMoroso.Add(CargarItemMoroso(obj[i].accountInfoList[0], dtNow, vueltaMorosoPhones));
                    esErrorEnMoroso = false;

                    placementSeccion = "agencyInfo";
                    listCartera.Add(CargarItemCartera(obj[i].accountInfoList[0], dtNow));
                    esErrorEnCartera = false;


                    string idMoroso = "RMS" + obj[i].accountInfoList[0].accountNumber;
                    string idcliente = "AMEX_" + obj[i].accountInfoList[0].agencyInfo.legacyRecoverCode;
                    MOROSOLAST morosolast = (from x in DataContextManager.Context.MOROSOLASTs
                                             where x.IDCLIENTE == idcliente
                                             && x.IDMOROSO == idMoroso
                                             select x).FirstOrDefault();
                    if (morosolast == null)
                    {
                        placementSeccion = "agencyInfo";
                        listMorosolast.Add(CargarItemMorosolast(obj[i].accountInfoList[0], dtNow));
                    }
                    esErrorMorosolast = false;

                    placementSeccion = "account";
                    listProducto.Add(CargarItemProducto(obj[i].accountInfoList[0], dtNow));
                    esErrorEnProducto = false;


                    if (obj[i].accountInfoList[0].financialSummary != null)
                    {
                        placementSeccion = "financialSummary";
                        listSkiptrace.Add(CargarItemSkipTrace(obj[i].accountInfoList[0], dtNow));
                        esErrorEnSkiptrace = false;
                    }


                    if (obj[i].accountInfoList[0].treatments != null)
                    {
                        placementSeccion = "treatments";
                        listSkiptrace.Add(CargarItemSkipTraceDesdeTreatments(obj[i].accountInfoList[0], DateTime.Now));
                        esErrorEnSkiptrace = false;
                    }



                    #region CargarItemAmexPagosMinimo -> customerFinancial || LOG DE PAGO

                    placementSeccion = "customerFinancial";
                    listAmexPagosMinimo.AddRange(CargarItemAmexPagosMinimo(obj[i].accountInfoList[0], dtNow));
                    esErrorEnAmexPagosMinimo = false;

                    //listAmexLogDePago.AddRange(CargarlistAmexLogDePago_desdePagosMinimo(obj[i].accountInfoList[0], dtNow));

                    #endregion

                    #region CargarItemListAmexInfoAdicA -> financialTransactionDetails || LOG DE PAGO

                    placementSeccion = "financialTransactionDetails";
                    listAmexInfoAdicA.AddRange(CargarItemListAmexInfoAdicA(obj[i].accountInfoList[0], dtNow));
                    esErrorAmexInfoAdicA = false;

                    //listAmexLogDePago.AddRange(CargarlistAmexLogDePago_desdeInfoAdicional(obj[i].accountInfoList[0], dtNow));

                    #endregion


                }

                DataContextManager.Context.MOROSOLASTs.InsertAllOnSubmit(listMorosolast);
                DataContextManager.Context.SKIPTRACEs.InsertAllOnSubmit(listSkiptrace);
                DataContextManager.Context.AMEX_PAGOS_MINIMOs.InsertAllOnSubmit(listAmexPagosMinimo);
                DataContextManager.Context.AMEX_INFO_ADIC_As.InsertAllOnSubmit(listAmexInfoAdicA);

                DataContextManager.Context.SubmitChanges();
            }
            catch (Exception ex)
            {
                resultVO.isError = true;
                resultVO.Mensaje = ex.Message + " vueltaGral: " + vueltaGral
                    + (esErrorEnMoroso ? ", Error en Moroso: " + esErrorEnMoroso : "")
                    + (vueltaMorosoPhones != 0 ? ", vueltaMorosoPhones: " + vueltaMorosoPhones : "")
                    + (esErrorEnCartera ? ", Error en Cartera: " : "")
                    + (esErrorEnProducto ? ", Error en Producto: " : "")
                    + (esErrorEnSkiptrace ? ", Error en Skiptrace: " : "")
                    + (esErrorEnAmexPagosMinimo ? ", Error en AmexPagosMinimo: " : "")
                    + (esErrorAmexInfoAdicA ? ", Error en Amex_Info_Adic_A: " : "")
                    + (". IdMoroso: " + moroso)
                    + (". placementSeccion: " + placementSeccion);
            }
            return resultVO;
        }
        #endregion

        #region Procesar Archivos Recall
        protected void btnProcesarArchivosRecall_Click(object sender, EventArgs e)
        {
            bool huboError = false;
            string msj = "La operación se realizó con éxito.";
            string relativePath = VirtualPathUtility.ToAbsolute("~/File/AMEX_ACORN/ejemplo.json");
            string path = Server.MapPath(relativePath);

            try
            {
                if (!fuArchivosAcornRecall.FileName.Contains("RECALL"))
                    throw new Exception("El archivo ingresado no es del tipo Recall.");

                if (fuArchivosAcornRecall.HasFile)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    fuArchivosAcornRecall.SaveAs(path);

                    System.IO.StreamReader file = new System.IO.StreamReader(path);
                    var json = file.ReadToEnd();
                    file.Close();

                    var serialize = new JavaScriptSerializer();
                    serialize.RegisterConverters(new[] { new DynamicJsonConverter() });

                    dynamic obj = serialize.Deserialize(json, typeof(object));
                    ResultVO resultVO = CargarObjetoRecall(obj.accountRecallInfoList);

                    if (resultVO.isError)
                    {
                        string mesj = "Ocurio un error al procesar el archivo Recall. " + resultVO.Mensaje;
                        throw new Exception(mesj);
                    }
                    msj += resultVO.Mensaje;
                }
                else
                {
                    throw new Exception("No se cargaron archivos.");
                }
            }
            catch (Exception ex)
            {
                huboError = true;
                msj = ex.Message;
            }
            MostrardivLbError(huboError, msj, divRowLbErrorRecall, divLbErrorRecall, lbErrorRecall);

            CrearArchivoDeRecepcion(fuArchivosAcornRecall.FileName, path, "RECALL", "ACORNMORECALL");
        }
        protected ResultVO CargarObjetoRecall(dynamic obj)
        {
            int vueltaGral = 0;
            bool esErrorEnSkiptrace = false;
            string moroso = "";
            ResultVO resultVO = new ResultVO()
            {
                isError = false,
                Mensaje = ""
            };

            try
            {
                string msjMorososRechazado = "";
                List<WEBAPP.DataAccess.SKIPTRACE> listSkiptrace = new List<WEBAPP.DataAccess.SKIPTRACE>();
                for (int i = 0; i < obj.Count; i++)
                {
                    DateTime dtNow = DateTime.Now;
                    ++vueltaGral;
                    moroso = "RMS" + obj[i].accountNumber;
                    //No puede no estar, pero en el caso de que pase ignorarlo y poner en leyenda
                    if (!DataContextManager.Context.MOROSOs.Any(c => c.IDMOROSO == moroso))
                    {
                        msjMorososRechazado += obj[i].accountNumber + ", ";
                        continue;
                    }
                    string DATA = "";
                    StringBuilder sb = new StringBuilder();
                    sb.Append("RMS" + obj[i].accountNumber);
                    sb.Append("updateDate: " + Convert.ToDateTime(obj[i].updateDate));
                    sb.Append("agencyId: " + obj[i].agencyId);
                    sb.Append("agencyAssignmentStatusCode: " + obj[i].agencyAssignmentStatusCode);
                    sb.Append("agencyRecallDate: " + Convert.ToDateTime(obj[i].agencyRecallDate));
                    sb.Append("balanceAmount: " + obj[i].balanceAmount);
                    DATA = sb.ToString();
                    WEBAPP.DataAccess.SKIPTRACE skiptrace = MapearItemSkipTrace(moroso, dtNow, dtNow.ToString("HH:mm:ss"), "Recall", DATA);
                    listSkiptrace.Add(skiptrace);
                    esErrorEnSkiptrace = false;
                }
                if (!string.IsNullOrEmpty(msjMorososRechazado))
                    resultVO.Mensaje += "Morosos rechazados: " + msjMorososRechazado;

                DataContextManager.Context.SKIPTRACEs.InsertAllOnSubmit(listSkiptrace);
                DataContextManager.Context.SubmitChanges();
            }
            catch (Exception ex)
            {
                resultVO.isError = true;
                resultVO.Mensaje = ex.Message + " vueltaGral: " + vueltaGral
                    + (esErrorEnSkiptrace ? ", Error en Skiptrace: " : "")
                    + (". IdMoroso: " + moroso);
            }
            return resultVO;
        }
        #endregion

        #region Procesar Archivos Update
        protected void btnProcesarArchivosUpdate_Click(object sender, EventArgs e)
        {
            bool huboError = false;
            string msj = "";
            DateTime dtNow = DateTime.Now;
            string relativePath = VirtualPathUtility.ToAbsolute("~/File/AMEX_ACORN/ejemplo.json");
            string path = Server.MapPath(relativePath);

            try
            {
                if (!fuArchivosAcornUpdate.FileName.ToUpper().Contains("UPDATES"))
                    throw new Exception("El archivo ingresado no es del tipo Update.");

                if (fuArchivosAcornUpdate.HasFile)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    fuArchivosAcornUpdate.SaveAs(path);

                    System.IO.StreamReader file = new System.IO.StreamReader(path);
                    var json = file.ReadToEnd();
                    file.Close();

                    var serialize = new JavaScriptSerializer();
                    serialize.RegisterConverters(new[] { new DynamicJsonConverter() });

                    dynamic obj = serialize.Deserialize(json, typeof(object));
                    ResultVO resultVO = CargarObjetoUpdate(obj, dtNow);

                    if (resultVO.isError)
                    {
                        string mesj = "Ocurio un error al procesar el archivo Update. " + resultVO.Mensaje;
                        throw new Exception(mesj);
                    }

                    msj += resultVO.Mensaje;
                }
                else
                {
                    throw new Exception("No se cargaron archivos.");
                }
            }
            catch (Exception ex)
            {
                huboError = true;
                msj = ex.Message + (countTest == 0 ? "" : "countTest: " + countTest) + (string.IsNullOrEmpty(updateSeccion) ? "" : ". updateSeccion: " + updateSeccion);
            }
            MostrardivLbError(huboError, msj, divRowLbErrorUpdate, divLbErrorUpdate, lbErrorUpdate);

            if (!huboError)
                CrearArchivoDeRecepcion(fuArchivosAcornUpdate.FileName, path, "UPDATESOB", "ACORNMOUPDOB");
        }
        protected ResultVO CargarObjetoUpdate(dynamic obj, DateTime dtNow)
        {
            int vueltaGral = 0;
            bool esErrorEnSkiptrace = false;
            string morosoDeError = "";
            ResultVO resultVO = new ResultVO()
            {
                isError = false,
                Mensaje = "La operación se realizo con éxito."
            };
            try
            {
                #region
                //No puede no estar, pero en el caso de que pase ignorarlo y poner en leyenda
                List<string> listIdMoroso = new List<string>();

                #region obtener/llenar listIdMoroso accountList

                if (obj.accountList == null)
                    throw new Exception("No existe la entidad AccountList");

                for (int i = 0; i < obj.accountList.Count; i++)
                {
                    #region comentario
                    //var objAccountDetailsListUpdateDate = obj.accountDetailsList[i].updateDate;
                    //var objAccountDetailsListUpdatedBy = obj.accountDetailsList[i].updatedBy;
                    //var objAccountDetailsListCancelCode = obj.accountDetailsList[i].cancelCode;
                    //var objAccountDetailsListCrDate = obj.accountDetailsList[i].crDate;
                    //var objAccountDetailsListCancellationDate = obj.accountDetailsList[i].cancellationDate;
                    //var objAccountDetailsListCardOpenDate = obj.accountDetailsList[i].cardOpenDate;
                    //var objAccountDetailsListLastChargeActivity = obj.accountDetailsList[i].lastChargeActivity;
                    #endregion
                    var objAccountDetailsListAccountNumber = "RMS" + obj.accountList[i].accountNumber;
                    listIdMoroso.Add(objAccountDetailsListAccountNumber);
                }


                #endregion

                string msjMorososRechazado = "";
                string msjProductoRechazado = "";
                string msjCarteraRechazado = "";

                List<WEBAPP.DataAccess.SKIPTRACE> listSkiptrace = new List<DataAccess.SKIPTRACE>();
                List<AMEX_LOG_DE_PAGO> listAmexLogDePago = new List<AMEX_LOG_DE_PAGO>();
                List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO> listAmexPagosMinimos = new List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO>();
                List<AMEX_INFO_ADIC_A> listAmexInfoAdicA = new List<AMEX_INFO_ADIC_A>();
                listIdMoroso = listIdMoroso.Distinct().ToList();

                List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO> listAmexPagosMinimo = (from x in DataContextManager.Context.AMEX_PAGOS_MINIMOs
                                                                                 where listIdMoroso.Contains(x.IdMoroso)
                                                                                 select x).ToList();
                foreach (string idMoroso in listIdMoroso)
                {
                    ++vueltaGral;

                    morosoDeError = idMoroso;

                    #region   MOROSO
                    MOROSO moroso = (from x in DataContextManager.Context.MOROSOs
                                     where x.IDMOROSO == idMoroso
                                     select x).FirstOrDefault();
                    if (moroso == null)
                    {
                        msjMorososRechazado += idMoroso + ", ";
                        continue;
                    }

                    if (obj.demographicsList != null)
                    {
                        updateSeccion = "demographicsList";
                        for (int i = 0; i < obj.demographicsList.Count; i++)
                        {
                            if (idMoroso == "RMS" + obj.demographicsList[i].accountNumber)
                            {
                                if (!string.IsNullOrEmpty(moroso.NOMBRE) && !(obj.demographicsList[i].firstName == null || obj.demographicsList[i].lastName == null))
                                {
                                    moroso.NOMBRE = obj.demographicsList[i].firstName + " " + obj.demographicsList[i].lastName;
                                }
                                #region     obj.demographicsList[i].phones
                                if (obj.demographicsList[i].phones != null)
                                {
                                    for (int j = 0; j < obj.demographicsList[i].phones.Count; j++)
                                    {
                                        //var objDemographicsListPhonesTypeCode = obj.demographicsList[i].phones[j].typeCode;
                                        //var objDemographicsListPhonesCategoryCode = obj.demographicsList[i].phones[j].categoryCode;
                                        //var objDemographicsListPhonesStatusCode = obj.demographicsList[i].phones[j].statusCode;
                                        //var objDemographicsListPhonesNumber = obj.demographicsList[i].phones[j].number;
                                        //var objDemographicsListPhonesLastContactDate = obj.demographicsList[i].phones[j].lastContactDate;
                                        #region telefonos
                                        if ((string.IsNullOrWhiteSpace(moroso.TELPART) || string.IsNullOrEmpty(moroso.TELPART))
                                            && obj.demographicsList[i].phones[j].typeCode == "H-HOME"
                                            && obj.demographicsList[i].phones[j].number != null)
                                        {
                                            moroso.TELPART = obj.demographicsList[i].phones[j].number.Trim();
                                        }

                                        if ((string.IsNullOrWhiteSpace(moroso.TELCOM) || string.IsNullOrEmpty(moroso.TELCOM))
                                                    && obj.demographicsList[i].phones[j].number != null
                                                    && obj.demographicsList[i].phones[j].typeCode == "B-BUS")//&& obj.demographics.phones[j].typeCode == "WORK"
                                        {
                                            moroso.TELCOM = obj.demographicsList[i].phones[j].number.Trim();
                                        }

                                        if ((string.IsNullOrEmpty(moroso.TELCEL) || string.IsNullOrWhiteSpace(moroso.TELCEL))
                                            && obj.demographicsList[i].phones[j].number != null
                                            && obj.demographicsList[i].phones[j].typeCode == "MOBILE"
                                             )
                                        {
                                            moroso.TELCEL = obj.demographicsList[i].phones[j].number.Trim();
                                        }

                                        #endregion
                                    }
                                }

                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region //      Comentarios

                    #region PRODUCTO_AMEX_PRUEBA
                    //PRODUCTO_AMEX_PRUEBA producto = (from x in DataContextManager.Context.PRODUCTO_AMEX_PRUEBAs
                    //                                 where x.IDMOROSO == idMoroso
                    //                                 select x).FirstOrDefault();
                    //if (producto != null)
                    //{
                    //    //RTA: SI NO MANDA currentBalance NOI ACTUALIZA
                    //    //CONSULTAR     Ver como machear el producto xq IdMoroso y producto no alcanza (faltaria el idCliente)
                    //    #region Comentarios
                    //                            //PRODUCTO_AMEX_PRUEBA producto = (from x in DataContextManager.Context.PRODUCTOs
                    //    //                     where 1==1
                    //    //                     //&& x.IDMOROSO ==  idMoroso
                    //    //                     //&& x.IDPRODUCTO == idProducto
                    //    //                     select x).FirstOrDefault();
                    //    //producto.IDMOROSO = idMoroso;
                    //    //producto.CLIENTE = "";
                    //    //producto.IDPRODUCTO = idMoroso.Replace("RMS", "").Trim();
                    //    #endregion
                    //    //CONSULTAR     De donde sacar el currentBalance
                    //    producto.DEUDAVENCIDA = obj.accountList.currentBalance;
                    //    producto.TOTALDEUDA = obj.accountList.currentBalance;
                    //    producto.FAPERTURA = dtNow;
                    //    producto.FATRASO = dtNow;
                    //    //{0}producto.CUENTA = "OP" + obj.agencyInfo.legacyRecoverCode;
                    //}
                    //else
                    //{
                    //    msjProductoRechazado += idMoroso + ", ";
                    //}
                    #endregion

                    #region   CARTERA_AMEX_PRUEBA
                    //WEBAPP.DataAccess.CARTERA_AMEX_PRUEBA cartera = (from x in DataContextManager.Context.CARTERA_AMEX_PRUEBAs
                    //                                                 where x.IDMOROSO == idMoroso
                    //                                                 select x).FirstOrDefault();
                    //if (cartera == null)
                    //{
                    //    msjCarteraRechazado += idMoroso + ", ";
                    //    //cartera = new WEBAPP.DataAccess.CARTERA_AMEX_PRUEBA();
                    //}
                    //else
                    //{
                    //    List<string> valuesForGRCC = new List<string>(new string[] { "PALM", "PBLM", "PELM", "PFLM", "TNLM", "QRPM" });
                    //    List<string> valuesForRCP = new List<string>(new string[] { "PARM", "PBRM", "PNCM", "PWCM", "TNRM", "TNCM", "QRNM" });
                    //    string accountNumber = obj.accountNumber;

                    //    cartera.IDMOROSO = idMoroso;
                    //    //CONSULTAR     
                    //    //{0}cartera.IDCLIENTE = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
                    //    cartera.ESTADO = "NO VISTO";
                    //    //cartera.IDOPERADOR = "CICLO_" + idMoroso.Substring(9, 1);     //este no cambia
                    //    cartera.IDSUPERVISOR = "MARINELLI";       //se queda como id IDSUPERVISOR
                    //    cartera.FINGRESO = dtNow;
                    //    cartera.FASIGNACION = dtNow;
                    //}

                    #endregion

                    #region
                    //"addressLine2": "ADDRESS LINE 2 OLD", (DIRECCIONALT)
                    //ESTADO (NO VISTO)
                    //FILROC (IDOPERADOR) EL DECIMO DIGITO DIGITO DE IZQUIERDA A DERECHA DEL ACCOUNT NUMBER ,ES EL NUMERO DE CICLO Ej: 376600000800000 ("CICLO_8")
                    //FECHAINGRESO,FASIGNACION (DIA DE INSERCCION DEL CASO (GETDATE SQL))
                    //\--Lo mismo?
                    //"legacyRecoverCode" (CUANDO ESTE CAMPO ES IGUAL A PALM,PBLM,PELM,PFLM,TNLM,QRPM SON GRCC) FILTROA
                    //"legacyRecoverCode" (CUANDO ESTE CAMPO ES IGUAL A PARM,PBRM,PNCM,PWCM,TNRM,TNCM,QRNM SON RCP) FILTROA
                    //--Lo mismo?

                    //MES Y AÑO DE ASIGNACION DE LA CUENTA (FILTROB) Ej: SEPTIEMBRE 2017
                    //cartera.LLAMADAS = ;
                    //cartera.FCOMPROMISO1 = ;
                    //cartera.MONTOCOMPROMISO1 = ;
                    //cartera.FCOMPROMISO2 = ;
                    //cartera.MONTOCOMPROMISO2 = ;
                    //cartera.FCOMPROMISO3 = ;
                    //cartera.MONTOCOMPROMISO3 = ;
                    //cartera.MORADESDE = ;
                    //cartera.ACTIVO = ;
                    //-------------------
                    //cartera.WORKSTATUS = obj.
                    #endregion


                    #endregion

                    #region   SKIPTRACE_AMEX_PRUEBA
                    string DATA = "";
                    StringBuilder sb = new StringBuilder();

                    #region financialSummaryList

                    if (obj.financialSummaryList != null)
                    {
                        updateSeccion = "financialSummaryList";
                        for (int i = 0; i < obj.financialSummaryList.Count; i++)
                        {
                            if ("RMS" + obj.financialSummaryList[i].accountNumber == idMoroso)
                            {
                                #region Datos del moroso

                                //sb.Append("RMS" + obj.financialSummaryList[i].accountNumber + ", ");
                                //DATA += "RMS" + obj.financialSummaryList[i].accountNumber + ", ";
                                //{0}DATA += obj.demographics.firstName + " " + obj.demographics.lastName + ", RMS" + idMoroso + " ";
                                //No hay estos datos
                                //{0}sb.Append("Address: " + obj.demographics.addresses[0].addressLine1);
                                //{0}sb.Append("City: " + obj.demographics.addresses[0].city);
                                //{0}sb.Append("Zip Code" + obj.customerDetails.countryCode);
                                //{0}sb.Append("birth Date" + obj.demographics.birthDate);
                                //{0}sb.Append("Employer' name" + obj.employer.name);
                                //{0}sb.Append("Employer' address" + obj.employer.address);
                                //{0}sb.Append("Current Balance" + obj.account.currentBalance);

                                #endregion

                                #region financialSummaryList[i].debitSummaryInMonths
                                if (obj.financialSummaryList[i].debitSummaryInMonths != null)
                                {
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month01 != null) { sb.Append("Summary debits in Month01: " + obj.financialSummaryList[i].debitSummaryInMonths.Month01 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month02 != null) { sb.Append("Summary debits in Month02: " + obj.financialSummaryList[i].debitSummaryInMonths.Month02 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month03 != null) { sb.Append("Summary debits in Month03: " + obj.financialSummaryList[i].debitSummaryInMonths.Month03 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month04 != null) { sb.Append("Summary debits in Month04: " + obj.financialSummaryList[i].debitSummaryInMonths.Month04 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month05 != null) { sb.Append("Summary debits in Month05: " + obj.financialSummaryList[i].debitSummaryInMonths.Month05 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month06 != null) { sb.Append("Summary debits in Month06: " + obj.financialSummaryList[i].debitSummaryInMonths.Month06 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month07 != null) { sb.Append("Summary debits in Month07: " + obj.financialSummaryList[i].debitSummaryInMonths.Month07 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month08 != null) { sb.Append("Summary debits in Month08: " + obj.financialSummaryList[i].debitSummaryInMonths.Month08 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month09 != null) { sb.Append("Summary debits in Month09: " + obj.financialSummaryList[i].debitSummaryInMonths.Month09 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month10 != null) { sb.Append("Summary debits in Month10: " + obj.financialSummaryList[i].debitSummaryInMonths.Month10 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month11 != null) { sb.Append("Summary debits in Month11: " + obj.financialSummaryList[i].debitSummaryInMonths.Month11 + ", "); }
                                    if (obj.financialSummaryList[i].debitSummaryInMonths.Month12 != null) { sb.Append("Summary debits in Month12: " + obj.financialSummaryList[i].debitSummaryInMonths.Month12 + ". "); }
                                }
                                if (obj.financialSummaryList[i].debitSummaryInMonthsUSD != null)
                                {
                                    sb.Append("Debit summary in Months USD: " + obj.financialSummaryList[i].debitSummaryInMonthsUSD);
                                }
                                if (obj.financialSummaryList[i].accountBalanceInMonths != null)
                                {
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month01 != null) { sb.Append("Acct Balance in Month 01: " + obj.financialSummaryList[i].accountBalanceInMonths.Month01 + ", "); }
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month02 != null) { sb.Append("Acct Balance in Month 02: " + obj.financialSummaryList[i].accountBalanceInMonths.Month02 + ", "); }
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month03 != null) { sb.Append("Acct Balance in Month 03: " + obj.financialSummaryList[i].accountBalanceInMonths.Month03 + ", "); }
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month04 != null) { sb.Append("Acct Balance in Month 04: " + obj.financialSummaryList[i].accountBalanceInMonths.Month04 + ", "); }
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month05 != null) { sb.Append("Acct Balance in Month 05: " + obj.financialSummaryList[i].accountBalanceInMonths.Month05 + ", "); }
                                    if (obj.financialSummaryList[i].accountBalanceInMonths.Month06 != null) { sb.Append("Acct Balance in Month 06: " + obj.financialSummaryList[i].accountBalanceInMonths.Month06 + ". "); }
                                }
                                if (obj.financialSummaryList[i].accountBalanceInMonthsUSD != null)
                                {
                                    sb.Append(": " + obj.financialSummaryList[i].accountBalanceInMonthsUSD);
                                }
                                if (obj.financialSummaryList[i].summaryCreditInMonths != null)
                                {
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month01 != null) { sb.Append("Summary credit in Month 01: " + obj.financialSummaryList[i].summaryCreditInMonths.Month01 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month02 != null) { sb.Append("Summary credit in Month 02: " + obj.financialSummaryList[i].summaryCreditInMonths.Month02 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month03 != null) { sb.Append("Summary credit in Month 03: " + obj.financialSummaryList[i].summaryCreditInMonths.Month03 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month04 != null) { sb.Append("Summary credit in Month 04: " + obj.financialSummaryList[i].summaryCreditInMonths.Month04 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month05 != null) { sb.Append("Summary credit in Month 05: " + obj.financialSummaryList[i].summaryCreditInMonths.Month05 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month06 != null) { sb.Append("Summary credit in Month 06: " + obj.financialSummaryList[i].summaryCreditInMonths.Month06 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month07 != null) { sb.Append("Summary credit in Month 07: " + obj.financialSummaryList[i].summaryCreditInMonths.Month07 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month08 != null) { sb.Append("Summary credit in Month 08: " + obj.financialSummaryList[i].summaryCreditInMonths.Month08 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month09 != null) { sb.Append("Summary credit in Month 09: " + obj.financialSummaryList[i].summaryCreditInMonths.Month09 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month10 != null) { sb.Append("Summary credit in Month 10: " + obj.financialSummaryList[i].summaryCreditInMonths.Month10 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month11 != null) { sb.Append("Summary credit in Month 11: " + obj.financialSummaryList[i].summaryCreditInMonths.Month11 + ", "); }
                                    if (obj.financialSummaryList[i].summaryCreditInMonths.Month12 != null) { sb.Append("Summary credit in Month 12: " + obj.financialSummaryList[i].summaryCreditInMonths.Month12 + ". "); }
                                }
                                if (obj.financialSummaryList[i].accountAgingInMonths != null)
                                {
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month01 != null) { sb.Append("Acct Aging in Month 01: " + obj.financialSummaryList[i].accountAgingInMonths.Month01 + ", "); }
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month02 != null) { sb.Append("Acct Aging in Month 02: " + obj.financialSummaryList[i].accountAgingInMonths.Month02 + ", "); }
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month03 != null) { sb.Append("Acct Aging in Month 03: " + obj.financialSummaryList[i].accountAgingInMonths.Month03 + ", "); }
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month04 != null) { sb.Append("Acct Aging in Month 04: " + obj.financialSummaryList[i].accountAgingInMonths.Month04 + ", "); }
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month05 != null) { sb.Append("Acct Aging in Month 05: " + obj.financialSummaryList[i].accountAgingInMonths.Month05 + ", "); }
                                    if (obj.financialSummaryList[i].accountAgingInMonths.Month06 != null) { sb.Append("Acct Aging in Month 06: " + obj.financialSummaryList[i].accountAgingInMonths.Month06 + ". "); }
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region customerFinancialList

                    if (obj.customerFinancialList != null)
                    {
                        for (int j = 0; j < obj.customerFinancialList.Count; j++)
                        {
                            if ("RMS" + obj.customerFinancialList[j].accountNumber == idMoroso)
                            {
                                if (obj.customerFinancialList[j].cycleCut != null)
                                {
                                    sb.Append("cycleCut: " + obj.customerFinancialList[j].cycleCut + ". ");
                                }
                                if (obj.customerFinancialList[j].otherIncome != null)
                                {
                                    sb.Append("OtherIncome: " + obj.customerFinancialList[j].otherIncome + ". ");
                                }
                                if (obj.customerFinancialList[j].totalPastdue != null)
                                {
                                    sb.Append("TotalPastdue: " + obj.customerFinancialList[j].totalPastdue + ". ");
                                }
                            }
                        }
                    }


                    #endregion

                    DATA += sb.ToString();
                    if (!string.IsNullOrEmpty(DATA))
                    {
                        DateTime dt = DateTime.Now;
                        listSkiptrace.Add(MapearItemSkipTrace(idMoroso, dt, dt.ToString("HH:mm:ss"), "UPDATE", DATA));
                    }
                    #endregion

                    #region AMEX_INFO_ADIC_A

                    if (obj.financialTransactionDetailsList != null)
                    {
                        updateSeccion = "financialTransactionDetailsList";
                        for (int i = 0; i < obj.financialTransactionDetailsList.Count; i++)
                        {
                            var updateDate = obj.financialTransactionDetailsList[i].updateDate;
                            var updatedBy = obj.financialTransactionDetailsList[i].updatedBy;
                            var accountNumber = obj.financialTransactionDetailsList[i].accountNumber;

                            if (idMoroso == "RMS" + obj.financialTransactionDetailsList[i].accountNumber)
                            {
                                List<ItemTransactions> listItemTransactions = new List<ItemTransactions>();

                                for (int j = 0; j < obj.financialTransactionDetailsList[i].transactions.Count; j++)
                                {

                                    ///Ignorar las transactions que repitan transactionId, transactionDate, transactionAmount
                                    ///Para lo transactionTypeCode== "Payment" y transactionCode == 50||52||11||12
                                    ///
                                    ///Y la acornPostDate va a pasar a ser una primary key de la transaction (a confirmar)
                                    ///
                                    ///Y boton de pago Campos idmoroso, Datetime.now, transactionCode;transactionDate;transactionAmount;currency;foreignTransactionAmount;

                                    #region datos
                                    var transactionsUpdateDate = obj.financialTransactionDetailsList[i].transactions[j].updateDate;
                                    var transactionsUpdatedBy = obj.financialTransactionDetailsList[i].transactions[j].updatedBy;
                                    var transactionsAccountCategoryTypeCode = obj.financialTransactionDetailsList[i].transactions[j].accountCategoryTypeCode;
                                    var last5DigitsOfTransaction = obj.financialTransactionDetailsList[i].transactions[j].last5DigitsOfTransaction;
                                    var transactionAmount = obj.financialTransactionDetailsList[i].transactions[j].transactionAmount;
                                    var transactionTypeCode = obj.financialTransactionDetailsList[i].transactions[j].transactionTypeCode;
                                    var transactionDate = obj.financialTransactionDetailsList[i].transactions[j].transactionDate;
                                    var legacyFieldCode = obj.financialTransactionDetailsList[i].transactions[j].legacyFieldCode;
                                    var transactionCode = obj.financialTransactionDetailsList[i].transactions[j].transactionCode;
                                    var transactionId = obj.financialTransactionDetailsList[i].transactions[j].transactionId;
                                    var currency = obj.financialTransactionDetailsList[i].transactions[j].currency;
                                    var transactionReferId = obj.financialTransactionDetailsList[i].transactions[j].transactionReferId;
                                    var foreignTransactionAmount = obj.financialTransactionDetailsList[i].transactions[j].foreignTransactionAmount;
                                    var foreignCurrencyCode = obj.financialTransactionDetailsList[i].transactions[j].foreignCurrencyCode;
                                    var stampedFxRate = obj.financialTransactionDetailsList[i].transactions[j].stampedFxRate;
                                    var sorTransactionCode = obj.financialTransactionDetailsList[i].transactions[j].sorTransactionCode;
                                    var acornPostDate = obj.financialTransactionDetailsList[i].transactions[j].acornPostDate;
                                    var processingStatus = obj.financialTransactionDetailsList[i].transactions[j].processingStatus;

                                    var currentForeignBalance = obj.financialTransactionDetailsList[i].transactions[j].currentForeignBalance;
                                    var currentLocalCurrencyBalance = obj.financialTransactionDetailsList[i].transactions[j].currentLocalCurrencyBalance;
                                    var accountCategoryTypeCode = obj.financialTransactionDetailsList[i].transactions[j].accountCategoryTypeCode;
                                    #endregion


                                    ItemTransactions ItemTransactions = new ItemTransactions();
                                    ItemTransactions.transactionId = transactionId;
                                    ItemTransactions.transactionDate = transactionDate;
                                    ItemTransactions.transactionAmount = transactionAmount != null ? transactionAmount.ToString() : "";

                                    List<string> listConcat = listItemTransactions.Select(c => c.concat).ToList();
                                    if (listConcat.Contains(ItemTransactions.concat))
                                        continue;

                                    listItemTransactions.Add(ItemTransactions);


                                    //todos los casos (11,12,50,52)
                                    //transactionAmount	
                                    //if(transactionAmount==null)
                                    //    transactionAmount



                                    if (transactionAmount != null)
                                    {
                                        //currentLocalCurrencyBalance
                                        //transactionAmount
                                        string tipo = "";
                                        if (currentLocalCurrencyBalance != null)
                                            tipo = currentLocalCurrencyBalance.ToString();

                                        string monto = transactionAmount.ToString();

                                        AMEX_INFO_ADIC_A amexInfoAdicAEnPesos = new AMEX_INFO_ADIC_A();
                                        amexInfoAdicAEnPesos.IdMoroso = "RMS" + accountNumber;
                                        amexInfoAdicAEnPesos.Fecha = Convert.ToDateTime(transactionDate);
                                        amexInfoAdicAEnPesos.Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, true);
                                        amexInfoAdicAEnPesos.Monto = transactionAmount;
                                        amexInfoAdicAEnPesos.Descripcion = transactionTypeCode;
                                        amexInfoAdicAEnPesos.UpdateDate = transactionsUpdateDate != null? Convert.ToDateTime(transactionsUpdateDate): (DateTime?)null;
                                        listAmexInfoAdicA.Add(amexInfoAdicAEnPesos);

                                        listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
                                        {
                                            CargadoEn = dtNow,
                                            IdMoroso = amexInfoAdicAEnPesos.IdMoroso,
                                            Fecha = amexInfoAdicAEnPesos.Fecha,
                                            Tipo = amexInfoAdicAEnPesos.Tipo,
                                            Monto = amexInfoAdicAEnPesos.Monto,
                                            Descripcion = amexInfoAdicAEnPesos.Descripcion
                                        });
                                    }
                                    if (foreignTransactionAmount != null)
                                    {
                                        //currentForeignBalance
                                        //foreignTransactionAmount

                                        string tipo = "";
                                        if (currentLocalCurrencyBalance != null)
                                            tipo = currentLocalCurrencyBalance.ToString();
                                        string monto = "";
                                        if (transactionAmount != null)
                                            monto = transactionAmount.ToString();

                                        AMEX_INFO_ADIC_A amexInfoAdicAEnDolares = new AMEX_INFO_ADIC_A();
                                        amexInfoAdicAEnDolares.IdMoroso = "RMS" + accountNumber;
                                        amexInfoAdicAEnDolares.Fecha = Convert.ToDateTime(transactionDate);
                                        amexInfoAdicAEnDolares.Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, false);
                                        amexInfoAdicAEnDolares.Monto = foreignTransactionAmount != null ? foreignTransactionAmount : 0;
                                        amexInfoAdicAEnDolares.Descripcion = transactionTypeCode;
                                        amexInfoAdicAEnDolares.UpdateDate = transactionsUpdateDate != null ? Convert.ToDateTime(transactionsUpdateDate) : (DateTime?)null; 

                                        listAmexInfoAdicA.Add(amexInfoAdicAEnDolares);
                                        listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
                                        {
                                            CargadoEn = dtNow,
                                            IdMoroso = amexInfoAdicAEnDolares.IdMoroso,
                                            Fecha = amexInfoAdicAEnDolares.Fecha,
                                            Tipo = amexInfoAdicAEnDolares.Tipo,
                                            Monto = amexInfoAdicAEnDolares.Monto,
                                            Descripcion = amexInfoAdicAEnDolares.Descripcion
                                        });
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region AMEX_PAGOS_MINIMO_AMEX_PRUEBA

                    if (obj.customerFinancialList != null)
                    {
                        updateSeccion = "customerFinancialList";
                        //string IdCliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
                        for (int i = 0; i < obj.customerFinancialList.Count; i++)
                        {
                            //var objBankruptcyDetailsListAccountNumber = obj.bankruptcyDetailsList[i].accountNumber;
                            if (idMoroso == "RMS" + obj.customerFinancialList[i].accountNumber)
                            {
                                #region Comentarios
                                //var objCustomerFinancialListUpdateDate = obj.customerFinancialList[i].updateDate;
                                //var objCustomerFinancialListUpdatedBy = obj.customerFinancialList[i].updatedBy;
                                //var objCustomerFinancialListAccountNumber = obj.customerFinancialList[i].accountNumber;
                                //var objCustomerFinancialListCycleCut = obj.customerFinancialList[i].cycleCut;
                                //var objCustomerFinancialListTotalExposure = obj.customerFinancialList[i].totalExposure;
                                //var objCustomerFinancialListMinimumAmountDue = obj.customerFinancialList[i].minimumAmountDue;
                                //var objCustomerFinancialListUnbilledAmountDue = obj.customerFinancialList[i].unbilledAmountDue;
                                //var objCustomerFinancialListCurrentAmountDue = obj.customerFinancialList[i].currentAmountDue;
                                //var objCustomerFinancialListAmountDue30Days = obj.customerFinancialList[i].amountDue30Days;
                                //var objCustomerFinancialListAmountDue60Days = obj.customerFinancialList[i].amountDue60Days;
                                //var objCustomerFinancialListAmountDue90Days = obj.customerFinancialList[i].amountDue90Days;
                                //var objCustomerFinancialListAmountDue120Days = obj.customerFinancialList[i].amountDue120Days;
                                //var objCustomerFinancialListAmountDue150Days = obj.customerFinancialList[i].amountDue150Days;
                                #endregion

                                WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnPesos = (from x in listAmexPagosMinimo
                                                                                              where x.EsPesos == true
                                                                                              && x.IdMoroso == idMoroso
                                                                                              //&& x.IdCliente == IdCliente
                                                                                              select x).FirstOrDefault();
                                bool noExisteAmexPagosMinimoEnPesos = amexPagosMinimoEnPesos == null;
                                if (noExisteAmexPagosMinimoEnPesos)
                                {
                                    amexPagosMinimoEnPesos = new AMEX_PAGOS_MINIMO();
                                    amexPagosMinimoEnPesos.EsPesos = true;
                                    amexPagosMinimoEnPesos.IdMoroso = idMoroso;
                                    amexPagosMinimoEnPesos.IdCliente = "_";
                                }

                                if (obj.customerFinancialList[i].currentAmountDue != null)
                                {
                                    amexPagosMinimoEnPesos.Corriente = obj.customerFinancialList[i].currentAmountDue;
                                }
                                if (obj.customerFinancialList[i].minimumAmountDue != null)
                                {
                                    amexPagosMinimoEnPesos.PagoMinimo = obj.customerFinancialList[i].minimumAmountDue;
                                }
                                if (obj.customerFinancialList[i].amountDue30Days != null)
                                {
                                    amexPagosMinimoEnPesos.Dias30 = obj.customerFinancialList[i].amountDue30Days;
                                }
                                if (obj.customerFinancialList[i].amountDue60Days != null)
                                {
                                    amexPagosMinimoEnPesos.Dias60 = obj.customerFinancialList[i].amountDue60Days;
                                }
                                if (obj.customerFinancialList[i].amountDue90Days != null)
                                {
                                    amexPagosMinimoEnPesos.Dias90 = obj.customerFinancialList[i].amountDue90Days;
                                }
                                if (obj.customerFinancialList[i].amountDue120Days != null)
                                {
                                    amexPagosMinimoEnPesos.Dias120 = obj.customerFinancialList[i].amountDue120Days;
                                }

                                if (noExisteAmexPagosMinimoEnPesos)
                                {
                                    listAmexPagosMinimos.Add(amexPagosMinimoEnPesos);
                                }

                                if (obj.customerFinancialList[i].currentAmountDueInUSD != null
                                    || obj.customerFinancialList[i].amountDue30DaysInUSD != null
                                    || obj.customerFinancialList[i].amountDue60DaysInUSD != null
                                    || obj.customerFinancialList[i].amountDue90DaysInUSD != null
                                    || obj.customerFinancialList[i].amountDue120DaysInUSD != null)
                                {
                                    WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnDolar = (from x in listAmexPagosMinimo
                                                                                                  where x.EsPesos == false
                                                                                                  && x.IdMoroso == idMoroso
                                                                                                  //&& x.IdCliente == IdCliente
                                                                                                  select x).FirstOrDefault();
                                    bool NoExisteAmexPagosMinimoEnDolar = amexPagosMinimoEnDolar == null;
                                    if (NoExisteAmexPagosMinimoEnDolar)
                                    {
                                        amexPagosMinimoEnDolar = new AMEX_PAGOS_MINIMO();
                                        amexPagosMinimoEnDolar.EsPesos = false;
                                        amexPagosMinimoEnDolar.IdMoroso = idMoroso;
                                        amexPagosMinimoEnDolar.IdCliente = "_";
                                    }

                                    if (obj.customerFinancialList[i].currentAmountDueInUSD != null)
                                    {
                                        amexPagosMinimoEnDolar.Corriente = obj.customerFinancialList[i].currentAmountDueInUSD;
                                    }
                                    else
                                    {
                                        amexPagosMinimoEnDolar.Corriente = Convert.ToDecimal("0.00");
                                    }

                                    if (obj.customerFinancialList[i].amountDue30DaysInUSD != null)
                                    {
                                        amexPagosMinimoEnDolar.Dias30 = obj.customerFinancialList[i].amountDue30DaysInUSD;
                                    }
                                    else
                                    {
                                        amexPagosMinimoEnDolar.Dias30 = Convert.ToDecimal("0.00");
                                    }

                                    if (obj.customerFinancialList[i].amountDue60DaysInUSD != null)
                                    {
                                        amexPagosMinimoEnDolar.Dias60 = obj.customerFinancialList[i].amountDue60DaysInUSD;
                                    }
                                    else
                                    {
                                        amexPagosMinimoEnDolar.Dias60 = Convert.ToDecimal("0.00");
                                    }

                                    if (obj.customerFinancialList[i].amountDue90DaysInUSD != null)
                                    {
                                        amexPagosMinimoEnDolar.Dias90 = obj.customerFinancialList[i].amountDue90DaysInUSD;
                                    }
                                    else
                                    {
                                        amexPagosMinimoEnDolar.Dias90 = Convert.ToDecimal("0.00");
                                    }

                                    if (obj.customerFinancialList[i].amountDue120DaysInUSD != null)
                                    {
                                        amexPagosMinimoEnDolar.Dias120 = obj.customerFinancialList[i].amountDue120DaysInUSD;
                                    }
                                    else
                                    {
                                        amexPagosMinimoEnDolar.Dias120 = Convert.ToDecimal("0.00");
                                    }


                                    if (NoExisteAmexPagosMinimoEnDolar)
                                    {
                                        listAmexPagosMinimos.Add(amexPagosMinimoEnDolar);
                                    }
                                }
                            }
                        }
                    }
                    #endregion




                    #region     obj.bankruptcyDetailsList
                    //for (int i = 0; i < obj.bankruptcyDetailsList.Count; i++)
                    //{
                    //    var objBankruptcyDetailsListAccountNumber = obj.bankruptcyDetailsList[i].accountNumber;
                    //    if (idMoroso == obj.bankruptcyDetailsList[i].accountNumber)
                    //    {
                    //        var objBankruptcyDetailsListUpdateDate = obj.bankruptcyDetailsList[i].updateDate;
                    //        var objBankruptcyDetailsListUpdatedBy = obj.bankruptcyDetailsList[i].updatedBy;
                    //        var objBankruptcyDetailsListAssetIndicator = obj.bankruptcyDetailsList[i].assetIndicator;
                    //        var objBankruptcyDetailsListBankruptcyChapter = obj.bankruptcyDetailsList[i].bankruptcyChapter;
                    //    }
                    //}
                    #endregion

                    #region     obj.bankruptcyDetailsList
                    //for (int i = 0; i < obj.bankruptcyDetailsList.Count; i++)
                    //{
                    //    if (idMoroso == obj.bankruptcyDetailsList[i].accountNumber)
                    //    {
                    //        var objCollectionScoreListUpdateDate = obj.collectionScoreList[i].updateDate;
                    //        var objCollectionScoreListUpdatedBy = obj.collectionScoreList[i].updatedBy;
                    //        var objCollectionScoreListAccountNumber = obj.collectionScoreList[i].accountNumber;
                    //        var objCollectionScoreListRecoveryScore = obj.collectionScoreList[i].recoveryScore;
                    //        var objCollectionScoreListFicoScore = obj.collectionScoreList[i].ficoScore;
                    //    }
                    //}
                    #endregion

                    #region     obj.financialRiskList
                    //for (int i = 0; i < obj.financialRiskList.Count; i++)
                    //{
                    //    if (idMoroso == obj.financialRiskList[i].accountNumber)
                    //    {
                    //        var objFinancialRiskListUpdateDate = obj.financialRiskList[i].updateDate;
                    //        var objFinancialRiskListUpdatedBy = obj.financialRiskList[i].updatedBy;
                    //        var objFinancialRiskListAccountNumber = obj.financialRiskList[i].accountNumber;
                    //        var objFinancialRiskListMinDueIndicator = obj.financialRiskList[i].minDueIndicator;
                    //        var objFinancialRiskListTotalCustomerLevelMinDueCcsg = obj.financialRiskList[i].totalCustomerLevelMinDueCcsg;
                    //        var objFinancialRiskListTotalCustomerLevelMinDueOpen = obj.financialRiskList[i].totalCustomerLevelMinDueOpen;
                    //        var objFinancialRiskListTotalCustomerLevelMinDue = obj.financialRiskList[i].totalCustomerLevelMinDue;
                    //        var objFinancialRiskListTotalCourtCostsReimbursedToPreviousAgency = obj.financialRiskList[i].totalCourtCostsReimbursedToPreviousAgency;
                    //    }
                    //}
                    #endregion

                    #region obj.financialSummaryList
                    //for (int i = 0; i < obj.financialSummaryList.Count; i++)
                    //{
                    //    if (idMoroso == obj.financialSummaryList[i].accountNumber)
                    //    {
                    //        var objFinancialSummaryListUpdateDate = obj.financialSummaryList[i].updateDate;
                    //        var objFinancialSummaryListUpdatedBy = obj.financialSummaryList[i].updatedBy;
                    //        var objFinancialSummaryListAccountNumber = obj.financialSummaryList[i].accountNumber;
                    //        var objFinancialSummaryListDebitSummaryInMonths = obj.financialSummaryList[i].debitSummaryInMonths;
                    //
                    //        #region   obj.financialSummaryList[i].debitSummaryInMonths
                    //        if (obj.financialSummaryList[i].debitSummaryInMonths != null)
                    //        {
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth01 = obj.financialSummaryList[i].debitSummaryInMonths.Month01;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth02 = obj.financialSummaryList[i].debitSummaryInMonths.Month02;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth03 = obj.financialSummaryList[i].debitSummaryInMonths.Month03;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth04 = obj.financialSummaryList[i].debitSummaryInMonths.Month04;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth05 = obj.financialSummaryList[i].debitSummaryInMonths.Month05;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth06 = obj.financialSummaryList[i].debitSummaryInMonths.Month06;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth07 = obj.financialSummaryList[i].debitSummaryInMonths.Month07;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth08 = obj.financialSummaryList[i].debitSummaryInMonths.Month08;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth09 = obj.financialSummaryList[i].debitSummaryInMonths.Month09;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth10 = obj.financialSummaryList[i].debitSummaryInMonths.Month10;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth11 = obj.financialSummaryList[i].debitSummaryInMonths.Month11;
                    //            var objFinancialSummaryListDebitSummaryInMonthsMonth12 = obj.financialSummaryList[i].debitSummaryInMonths.Month12;
                    //        }
                    //        #endregion
                    //        #region     obj.financialSummaryList[i].accountBalanceInMonths
                    //        if (obj.financialSummaryList[i].accountBalanceInMonths != null)
                    //        {
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth01 = obj.financialSummaryList[i].accountBalanceInMonths.Month01;
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth02 = obj.financialSummaryList[i].accountBalanceInMonths.Month02;
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth03 = obj.financialSummaryList[i].accountBalanceInMonths.Month03;
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth04 = obj.financialSummaryList[i].accountBalanceInMonths.Month04;
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth05 = obj.financialSummaryList[i].accountBalanceInMonths.Month05;
                    //            var objFinancialSummaryListAccountBalanceInMonthsMonth06 = obj.financialSummaryList[i].accountBalanceInMonths.Month06;
                    //        }
                    //        #endregion
                    //        #region     obj.financialSummaryList[i].interestAccessedByMonth
                    //        if (obj.financialSummaryList[i].interestAccessedByMonth != null)
                    //        {
                    //            var objFinancialSummaryListInterestAccessedByMonthMonth07 = obj.financialSummaryList[i].interestAccessedByMonth.Month07;
                    //            var objFinancialSummaryListInterestAccessedByMonthMonth08 = obj.financialSummaryList[i].interestAccessedByMonth.Month08;
                    //            var objFinancialSummaryListInterestAccessedByMonthMonth09 = obj.financialSummaryList[i].interestAccessedByMonth.Month09;
                    //            var objFinancialSummaryListInterestAccessedByMonthMonth10 = obj.financialSummaryList[i].interestAccessedByMonth.Month10;
                    //        }
                    //        #endregion
                    //        #region     obj.financialSummaryList[i].summaryCreditInMonths
                    //        if (obj.financialSummaryList[i].summaryCreditInMonths != null)
                    //        {
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth01 = obj.financialSummaryList[i].summaryCreditInMonths.Month01;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth02 = obj.financialSummaryList[i].summaryCreditInMonths.Month02;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth03 = obj.financialSummaryList[i].summaryCreditInMonths.Month03;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth04 = obj.financialSummaryList[i].summaryCreditInMonths.Month04;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth05 = obj.financialSummaryList[i].summaryCreditInMonths.Month05;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth06 = obj.financialSummaryList[i].summaryCreditInMonths.Month06;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth07 = obj.financialSummaryList[i].summaryCreditInMonths.Month07;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth08 = obj.financialSummaryList[i].summaryCreditInMonths.Month08;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth09 = obj.financialSummaryList[i].summaryCreditInMonths.Month09;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth10 = obj.financialSummaryList[i].summaryCreditInMonths.Month10;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth11 = obj.financialSummaryList[i].summaryCreditInMonths.Month11;
                    //            var objFinancialSummaryListSummaryCreditInMonthsMonth12 = obj.financialSummaryList[i].summaryCreditInMonths.Month12;
                    //        }
                    //        #endregion
                    //        #region     obj.financialSummaryList[i].accountAgingInMonths
                    //        if (obj.financialSummaryList[i].accountAgingInMonths != null)
                    //        {
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth01 = obj.financialSummaryList[i].accountAgingInMonths.Month01;
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth02 = obj.financialSummaryList[i].accountAgingInMonths.Month02;
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth03 = obj.financialSummaryList[i].accountAgingInMonths.Month03;
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth04 = obj.financialSummaryList[i].accountAgingInMonths.Month04;
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth05 = obj.financialSummaryList[i].accountAgingInMonths.Month05;
                    //            var objFinancialSummaryListAccountAgingInMonthsMonth06 = obj.financialSummaryList[i].accountAgingInMonths.Month06;
                    //        }
                    //        #endregion
                    //    }
                    //}
                    #endregion

                    #region     obj.productDetailsList
                    //for (int i = 0; i < obj.productDetailsList.Count; i++)
                    //{
                    //    if (idMoroso == obj.productDetailsList[i].accountNumber)
                    //    {
                    //        var objProductDetailsListUpdateDate = obj.productDetailsList[i].updateDate;
                    //        var objProductDetailsListUpdatedBy = obj.productDetailsList[i].updatedBy;
                    //        var objProductDetailsListAccountNumber = obj.productDetailsList[i].accountNumber;
                    //        var objProductDetailsListFullProductDescription = obj.productDetailsList[i].fullProductDescription;
                    //    }
                    //}
                    #endregion

                    #region     obj.riskScoreList
                    //for (int i = 0; i < obj.riskScoreList.Count; i++)
                    //{
                    //    var objRiskScoreListAccountNumber = obj.riskScoreList[i].accountNumber;
                    //    if (idMoroso == obj.riskScoreList[i].accountNumber)
                    //    {
                    //        var objRiskScoreListUpdateDate = obj.riskScoreList[i].updateDate;
                    //        var objRiskScoreListUpdatedBy = obj.riskScoreList[i].updatedBy;
                    //        var objRiskScoreListCustomerLevelScoreForChargeAndLending = obj.riskScoreList[i].customerLevelScoreForChargeAndLending;
                    //        var objRiskScoreListLendingPayToCurrrentScore = obj.riskScoreList[i].lendingPayToCurrrentScore;
                    //    }
                    //}
                    #endregion

                    #region     obj.strategyInfoList
                    //for (int i = 0; i < obj.strategyInfoList.Count; i++)
                    //{
                    //    var objStrategyInfoListAccountNumber = obj.strategyInfoList[i].accountNumber;
                    //    if (idMoroso == objStrategyInfoListAccountNumber)
                    //    {
                    //        var objStrategyInfoListUpdateDate = obj.strategyInfoList[i].updateDate;
                    //        var objStrategyInfoListUpdatedBy = obj.strategyInfoList[i].updatedBy;
                    //        var objStrategyInfoListStrategyIndicator1 = obj.strategyInfoList[i].strategyIndicator1;
                    //        var objStrategyInfoListStrategyIndicator2 = obj.strategyInfoList[i].strategyIndicator2;
                    //        var objStrategyInfoListStrategyIndicator3 = obj.strategyInfoList[i].strategyIndicator3;
                    //        var objStrategyInfoListStrategyIndicator4 = obj.strategyInfoList[i].strategyIndicator4;
                    //        var objStrategyInfoListNoOfCcsgChargeCardsCustomerHas = obj.strategyInfoList[i].noOfCcsgChargeCardsCustomerHas;
                    //        var objStrategyInfoListNoOfCcsgLendingCardsCustomerHas = obj.strategyInfoList[i].noOfCcsgLendingCardsCustomerHas;
                    //        var objStrategyInfoListSegmentId1 = obj.strategyInfoList[i].segmentId1;
                    //        var objStrategyInfoListSegmentId2 = obj.strategyInfoList[i].segmentId2;
                    //        var objStrategyInfoListSegmentId3 = obj.strategyInfoList[i].segmentId3;
                    //        var objStrategyInfoListNoOfOpenCardsCustomerHas = obj.strategyInfoList[i].noOfOpenCardsCustomerHas;
                    //        var objStrategyInfoListIndicatorForLocBalance = obj.strategyInfoList[i].indicatorForLocBalance;
                    //        var objStrategyInfoListReAgeIndicator = obj.strategyInfoList[i].reAgeIndicator;
                    //        var objStrategyInfoListCareEligibility = obj.strategyInfoList[i].careEligibility;
                    //        var objStrategyInfoListWrittenOffIndicator = obj.strategyInfoList[i].writtenOffIndicator;
                    //        var objStrategyInfoListLendingInCharge = obj.strategyInfoList[i].lendingInCharge;
                    //        var objStrategyInfoListSpeedToPayModel = obj.strategyInfoList[i].speedToPayModel;
                    //    }
                    //}
                    #endregion


                }

                DataContextManager.Context.SKIPTRACEs.InsertAllOnSubmit(listSkiptrace);
                DataContextManager.Context.AMEX_LOG_DE_PAGOs.InsertAllOnSubmit(listAmexLogDePago);
                DataContextManager.Context.AMEX_INFO_ADIC_As.InsertAllOnSubmit(listAmexInfoAdicA);
                DataContextManager.Context.AMEX_PAGOS_MINIMOs.InsertAllOnSubmit(listAmexPagosMinimos);
                DataContextManager.Context.SubmitChanges();

                esErrorEnSkiptrace = false;
                if (!string.IsNullOrEmpty(msjMorososRechazado))
                    resultVO.Mensaje += "Morosos rechazados: " + msjMorososRechazado;
                if (!string.IsNullOrEmpty(msjProductoRechazado))
                    resultVO.Mensaje += "Productos rechazados: " + msjProductoRechazado;
                if (!string.IsNullOrEmpty(msjCarteraRechazado))
                    resultVO.Mensaje += "Carteras rechazados: " + msjCarteraRechazado;
                #endregion
            }
            catch (Exception ex)
            {
                resultVO.isError = true;
                resultVO.Mensaje = ex.Message + " vueltaGral: " + vueltaGral
                    + (esErrorEnSkiptrace ? ", Error en Skiptrace: " : "")
                    + (". IdMoroso: " + morosoDeError)
                + (" . countTest: " + countTest)
                + (" . updateSeccion: " + updateSeccion);
            }
            return resultVO;
        }
        private string ObtenerTipoDePayment(string transactionTypeCode, string transactionCode)
        {
            string result = "";

            if (transactionTypeCode == "payment")
            {
                if (transactionCode == "11")
                {
                    result = "Débito / Ajuste";
                }
                else if (transactionCode == "12")
                {
                    result = "Crédito";
                }
                else if (transactionCode == "50")
                {
                    result = "Pago";
                }
                else if (transactionCode == "52")
                {
                    result = "Pago reverso";
                }
            }
            else if (transactionTypeCode == "Placement")
            {

            }

            return result;
        }
        #endregion

        #region Actions History
        protected void btnProcesarArchivosActionsHistory_Click(object sender, EventArgs e)
        {
            bool huboError = false;
            string msj = "La operación se realizó con éxito";
            string relativePath = VirtualPathUtility.ToAbsolute("~/File/AMEX_ACORN/ejemplo.json");
            string path = Server.MapPath(relativePath);

            try
            {
                if (!fuActionsHistory.FileName.Contains(""))
                    throw new Exception("El archivo ingresado no es del tipo ActionsHistory");

                if (fuActionsHistory.HasFile)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    fuActionsHistory.SaveAs(path);

                    System.IO.StreamReader file = new System.IO.StreamReader(path);
                    var json = file.ReadToEnd();
                    file.Close();

                    var serialize = new JavaScriptSerializer();
                    serialize.RegisterConverters(new[] { new DynamicJsonConverter() });

                    dynamic obj = serialize.Deserialize(json, typeof(object));

                    ResultVO resultVO = CargarObjetoActionsHistory(obj.actionsHistoryList);
                    if (resultVO.isError)
                    {
                        string mesj = "Ocurio un error al procesar el archivo Actions History. " + resultVO.Mensaje;
                        throw new Exception(mesj);
                    }
                }
                else
                {
                    throw new Exception("No se cargaron archivos");
                }
            }
            catch (Exception ex)
            {
                huboError = true;
                msj = ex.Message;
            }
            MostrardivLbError(huboError, msj, divRowLbErrorActionsHistory, divLbErrorActionsHistory, lbErrorActionsHistory);

            //if (!huboError)
            CrearArchivoDeRecepcion(fuActionsHistory.FileName, path, "", "HISTORY");
        }
        private ResultVO CargarObjetoActionsHistory(dynamic obj)
        {
            int vueltaGral = 0;
            bool esErrorEnSkiptrace = false;
            string moroso = "";
            ResultVO resultVO = new ResultVO()
            {
                isError = false,
                Mensaje = ""
            };

            try
            {
                string msjMorososRechazado = "";
                List<WEBAPP.DataAccess.SKIPTRACE> listSkiptrace = new List<WEBAPP.DataAccess.SKIPTRACE>();
                for (int i = 0; i < obj.Count; i++)
                {
                    DateTime dtNow = DateTime.Now;
                    ++vueltaGral;
                    moroso = "RMS" + obj[i].accountNumber;

                    if (!_test)
                    {
                        //No puede no estar, pero en el caso de que pase ignorarlo y poner en leyenda
                        if (!DataContextManager.Context.MOROSOs.Any(c => c.IDMOROSO == moroso))
                        {
                            msjMorososRechazado += obj[i].accountNumber + ", ";
                            continue;
                        }
                    }

                    string DATA = "";
                    #region Llenar DATA
                    StringBuilder sb = new StringBuilder();

                    if (!string.IsNullOrEmpty(obj[i].updateDate))
                    {
                        sb.Append("UpdateDate: " + Convert.ToDateTime(obj[i].updateDate));
                    }
                    if (!string.IsNullOrEmpty(obj[i].updatedBy))
                    {
                        sb.Append("UpdatedBy: " + obj[i].updatedBy);
                    }
                    if (!string.IsNullOrEmpty(obj[i].accountNumber))
                    {
                        sb.Append("AccountNumber: " + obj[i].accountNumber);
                    }
                    if (!string.IsNullOrEmpty(obj[i].accountCategoryTypeCode))
                    {
                        sb.Append("AccountCategoryTypeCode: " + obj[i].accountCategoryTypeCode);
                    }
                    if (!string.IsNullOrEmpty(obj[i].activityTypeCode))
                    {
                        sb.Append("ActivityTypeCode: " + MapearActivityTypeCode(obj[i].activityTypeCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].type))
                    {
                        sb.Append("Type: " + obj[i].type);
                    }
                    if (!string.IsNullOrEmpty(obj[i].contactTypeCode))
                    {
                        sb.Append("ContactTypeCode: " + MapearContactTypeCode(obj[i].contactTypeCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].partyContactCode))
                    {
                        sb.Append("PartyContactCode: " + MapearPartyContactCode(obj[i].partyContactCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].placeContactCode))
                    {
                        sb.Append("PlaceContactCode: " + MapearPlaceContactCode(obj[i].placeContactCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].promiseAmount))
                    {
                        sb.Append("PromiseAmount: " + obj[i].promiseAmount);
                    }
                    if (!string.IsNullOrEmpty(obj[i].promiseDueDate))
                    {
                        sb.Append("PromiseDueDate: " + Convert.ToDateTime(obj[i].promiseDueDate));
                    }
                    if (!string.IsNullOrEmpty(obj[i].promiseFrequencyCode))
                    {
                        sb.Append("PromiseFrequencyCode: " + MapearPromiseFrequencyCode(obj[i].promiseFrequencyCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].promisePaymentMethod))
                    {
                        sb.Append("PromisePaymentMethod: " + MapearPromisePaymentMethod(obj[i].promisePaymentMethod.ToString()));
                    }
                    if (!string.IsNullOrEmpty(obj[i].timeOfActivity))
                    {
                        sb.Append("TimeOfActivity: " + obj[i].timeOfActivity);
                    }
                    if (!string.IsNullOrEmpty(obj[i].note))
                    {
                        sb.Append("Note: " + obj[i].note);
                    }

                    DATA = sb.ToString();
                    #endregion

                    WEBAPP.DataAccess.SKIPTRACE skiptrace = MapearItemSkipTrace(moroso, dtNow, dtNow.ToString("HH:mm:ss"), "ActionsHistory", DATA);
                    listSkiptrace.Add(skiptrace);
                    esErrorEnSkiptrace = false;
                }

                if (!string.IsNullOrEmpty(msjMorososRechazado))
                    resultVO.Mensaje += "Morosos rechazados: " + msjMorososRechazado;

                DataContextManager.Context.SKIPTRACEs.InsertAllOnSubmit(listSkiptrace);
                DataContextManager.Context.SubmitChanges();
            }
            catch (Exception ex)
            {
                resultVO.isError = true;
                resultVO.Mensaje = ex.Message + " vueltaGral: " + vueltaGral
                    + (esErrorEnSkiptrace ? ", Error en Skiptrace: " : "")
                    + (". IdMoroso: " + moroso);
            }
            return resultVO;
        }

        #endregion

        #region CargarItem
        private static MOROSO CargarItemMoroso(dynamic obj, DateTime dtNow, int vueltaMorosoPhones)
        {
            //Demographics Key: accountNumber + accountCategoryTypeCode
            //Employer Key: accountNumber
            #region
            //var accountNumber = obj.accountNumber;
            //var customerDetails = obj.customerDetails;
            //var demographics = obj.demographics;
            //var demographics_firstName = obj.demographics.firstName;
            //var demographics_lastName = obj.demographics.lastName;
            //var demographics_addresses = obj.demographics.addresses;
            //var demographics_addresses_0 = obj.demographics.addresses[0];
            //var demographics_addresses_0_addressLine1 = obj.demographics.addresses[0].addressLine1;
            //var demographics_accountNumber = obj.demographics.accountNumber;

            //    //moroso.TELCOM = obj. ;
            //    //moroso.TELPART = obj. ;
            //    //moroso.TELCOMS = obj. ;
            //    //moroso.LOCALIDADALT = obj. ;
            //    //moroso.TELCEL = obj. ;
            //    //moroso.PROVINCIAALT = obj. ;
            //    //moroso.TELCELS = obj. ;
            //    //moroso.CODPOSTALT = obj. ;
            //    //moroso.TELALT1 = obj. ;
            //    //moroso.TELALT1S = obj. ;
            //    //moroso.TELALT2 = obj. ;
            //    //moroso.TELALT2S = obj. ;
            //    //moroso.TELALT3 = obj. ;
            //    //moroso.TELALT3S = obj. ;
            //    //moroso.EMAIL1 = obj. ;
            //    //moroso.DATOADIC = obj. ;
            #endregion


            string idMoroso = "RMS" + obj.accountNumber;
            MOROSO moroso = DataContextManager.Context.MOROSOs.Where(c => c.IDMOROSO == idMoroso).FirstOrDefault();

            if (moroso == null)
            {
                moroso = new MOROSO();
                moroso.IDMOROSO = "RMS" + obj.accountNumber;
                DataContextManager.Context.MOROSOs.InsertOnSubmit(moroso);
            }

            if (obj.demographics != null)
            {
                moroso.NOMBRE = obj.demographics.firstName + " " + obj.demographics.lastName;

                if (obj.demographics.addresses != null)
                {
                    //Puede venir mas de una direccion? Como se reconoce cada una?
                    moroso.DIRECCION = obj.demographics.addresses[0].addressLine1;
                    moroso.LOCALIDAD = obj.demographics.addresses[0].city;
                    moroso.PROVINCIA = obj.demographics.addresses[0].state;
                    moroso.CODPOST = obj.demographics.addresses[0].zipCode;
                }
                if (obj.demographics.phones != null)
                {
                    for (int j = 0; j < obj.demographics.phones.Count; j++)
                    {
                        ++vueltaMorosoPhones;
                        if (obj.demographics.phones[j].typeCode == "H-HOME"
                                && obj.demographics.phones[j].number != null
                                && (string.IsNullOrEmpty(moroso.TELPART) || string.IsNullOrWhiteSpace(moroso.TELPART)))
                        {
                            moroso.TELPART = obj.demographics.phones[j].number.Trim();
                            moroso.TELPARTS = 'N';
                        }

                        if (obj.demographics.phones[j].typeCode == "B-BUS"
                            && obj.demographics.phones[j].number != null
                            && (string.IsNullOrEmpty(moroso.TELCOM) || string.IsNullOrWhiteSpace(moroso.TELCOM)))
                        {
                            moroso.TELCOM = obj.demographics.phones[j].number.Trim();
                            moroso.TELCOMS = 'N';
                        }

                        if (obj.demographics.phones[j].typeCode == "MOBILE"
                            && obj.demographics.phones[j].number != null
                            && (string.IsNullOrEmpty(moroso.TELCEL) || string.IsNullOrWhiteSpace(moroso.TELCEL)))
                        {
                            moroso.TELCEL = obj.demographics.phones[j].number.Trim();
                            moroso.TELCELS = 'N';
                        }
                    }
                }
            }

            //Employer Key: accountNumber
            //Puede venir mas de una direccion? Como se reconoce cada una?
            if (obj.employer != null)
            {
                if (obj.employer.address != null)
                {
                    moroso.DIRECCIONALT = obj.employer.address.addressLine1;
                }
            }
            try
            {
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "demographics", AccountNumber: obj.accountNumber, AccountCategoryTypeCode: obj.AccountCategoryTypeCode);
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "employer", AccountNumber: obj.accountNumber);
            }
            catch (Exception ex) { }

            return moroso;
        }
        private static MOROSOLAST CargarItemMorosolast(dynamic obj, DateTime dtNow)
        {
            string idMoroso = "RMS" + obj.accountNumber;
            string idcliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;

            MOROSOLAST morosolast = new MOROSOLAST();
            morosolast.IDMOROSO = idMoroso;
            morosolast.IDCLIENTE = idcliente;
            morosolast.IDOPERADOR = null;
            morosolast.FECHA = (DateTime?)dtNow;
            morosolast.HORA = dtNow.ToString("HH:mm");
            morosolast.CONTACTGROUP = "";
            morosolast.CONTACT = "";
            morosolast.RESULTSET = "";
            morosolast.RESULT = "";
            morosolast.DESCRIPTION = "";
            morosolast.ACTION = "";
            morosolast.PERDATE = (DateTime?)null;
            morosolast.UNTILDATE = (DateTime?)null;
            morosolast.ACTIONCOUNT = (int?)null;

            return morosolast;
        }
        private static PRODUCTO CargarItemProducto(dynamic obj, DateTime dtNow)
        {
            //AgencyInfo Key: agencyId
            //Account Key: accountNumber + accountCategoryTypeCode

            #region
            //"PRODUCTO":
            //	"accountNumber": "(RMS) + 376006235152000" (IDMOROSO)
            //	"accountNumber": "376006235152000" (IDproducto - sin rms)
            //	"currentBalance": ej:34949.87 (DEUDA VENCIDA, TOTALDEUDA)
            //	FECHA QUE INGRESA EL CASO (FAPERTURA,FATRASO) (SERIA UN GETDATE EN SQL)
            //	(OP) + "legacyRecoverCode"  (CUENTA) 
            #endregion

            string idmoroso = "RMS" + obj.accountNumber.Trim();
            string idcliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode.Trim();
            string idproducto = obj.accountNumber.Trim();

            PRODUCTO producto = (from x in DataContextManager.Context.PRODUCTOs
                                 where x.IDCLIENTE.Trim() == idcliente
                                 && x.IDMOROSO.Trim() == idmoroso
                                 && x.IDPRODUCTO.Trim() == idproducto
                                 select x).FirstOrDefault();
            if (producto == null)
            {
                producto = new PRODUCTO();
                producto.IDMOROSO = idmoroso;
                producto.IDCLIENTE = idcliente;
                producto.IDPRODUCTO = idproducto;
                DataContextManager.Context.PRODUCTOs.InsertOnSubmit(producto);
            }

            producto.DEUDAVENCIDA = obj.account.currentBalance;
            producto.TOTALDEUDA = obj.account.currentBalance;
            producto.FAPERTURA = dtNow;
            producto.FATRASO = dtNow;
            producto.CUENTA = "OP" + obj.agencyInfo.legacyRecoverCode;

            try
            {
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "agencyInfo", AccountNumber: obj.accountNumber, AgencyId: obj.agencyInfo.agencyId);
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "Account", AccountNumber: obj.accountNumber, AccountCategoryTypeCode: obj.account.accountCategoryTypeCode);
            }
            catch (Exception ex) { }

            return producto;
        }
        private static WEBAPP.DataAccess.CARTERA CargarItemCartera(dynamic obj, DateTime dtNow)
        {
            #region comentarios
            //"addressLine2": "ADDRESS LINE 2 OLD", (DIRECCIONALT)
            //ESTADO (NO VISTO)
            //FILROC (IDOPERADOR) EL DECIMO DIGITO DIGITO DE IZQUIERDA A DERECHA DEL ACCOUNT NUMBER ,ES EL NUMERO DE CICLO Ej: 376600000800000 ("CICLO_8")
            //FECHAINGRESO,FASIGNACION (DIA DE INSERCCION DEL CASO (GETDATE SQL))

            //\--Lo mismo?
            //"legacyRecoverCode" (CUANDO ESTE CAMPO ES IGUAL A PALM,PBLM,PELM,PFLM,TNLM,QRPM SON GRCC) FILTROA
            //"legacyRecoverCode" (CUANDO ESTE CAMPO ES IGUAL A PARM,PBRM,PNCM,PWCM,TNRM,TNCM,QRNM SON RCP) FILTROA
            //--Lo mismo?

            //MES Y AÑO DE ASIGNACION DE LA CUENTA (FILTROB) Ej: SEPTIEMBRE 2017
            //cartera.LLAMADAS = ;
            //cartera.FCOMPROMISO1 = ;
            //cartera.MONTOCOMPROMISO1 = ;
            //cartera.FCOMPROMISO2 = ;
            //cartera.MONTOCOMPROMISO2 = ;
            //cartera.FCOMPROMISO3 = ;
            //cartera.MONTOCOMPROMISO3 = ;
            //cartera.MORADESDE = ;
            //cartera.ACTIVO = ;

            //-------------------

            //cartera.WORKSTATUS = obj.
            #endregion

            //AgencyInfo Key: agencyId
            List<string> valuesForGRCC = new List<string>(new string[] { "PALM", "PBLM", "PELM", "PFLM", "TNLM", "QRPM" });
            List<string> valuesForRCP = new List<string>(new string[] { "PARM", "PBRM", "PNCM", "PWCM", "TNRM", "TNCM", "QRNM" });
            string accountNumber = obj.accountNumber;

            string idmoroso = "RMS" + obj.accountNumber;
            string idcliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;

            WEBAPP.DataAccess.CARTERA cartera = (from x in DataContextManager.Context.CARTERAs
                                                 where x.IDMOROSO == idmoroso
                                                 && x.IDCLIENTE == idcliente
                                                 select x).FirstOrDefault();
            if (cartera == null)
            {
                cartera = new WEBAPP.DataAccess.CARTERA();
                cartera.IDMOROSO = idmoroso;
                cartera.IDCLIENTE = idcliente;
                DataContextManager.Context.CARTERAs.InsertOnSubmit(cartera);
            }
            //VER
            cartera.ESTADO = "NO VISTO";
            cartera.IDOPERADOR = "CICLO_" + accountNumber.Substring(9, 1);
            cartera.IDSUPERVISOR = "VIRTUAL";
            //VER
            cartera.FINGRESO = dtNow;
            //VER
            cartera.FASIGNACION = dtNow;

            string legacyRecoverCode = obj.agencyInfo.legacyRecoverCode;

            if (valuesForGRCC.Contains(legacyRecoverCode))
                cartera.FILTROA = "GRCC";

            if (valuesForRCP.Contains(legacyRecoverCode))
                cartera.FILTROA = "RCP";

            cartera.FILTROB = cartera.FASIGNACION.HasValue
                                ? cartera.FASIGNACION.Value.ToString("MMMM yyyy")
                                : "";

            try
            {
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "agencyInfo", AccountNumber: obj.accountNumber, AgencyId: obj.agencyInfo.agencyId);
            }
            catch (Exception ex) { }

            return cartera;
        }
        private static WEBAPP.DataAccess.SKIPTRACE CargarItemSkipTrace(dynamic obj, DateTime dtNow)
        {
            //Demographics Key: accountNumber + accountCategoryTypeCode
            //FinancialSummary Key: accountNumber + accountCategoryTypeCode
            //CustomerFinancial Key: accountNumber + accountCategoryTypeCode
            //Employer Key: accountNumber
            //Account Key: accountNumber + accountCategoryTypeCode

            #region StringBuilder sb = Data

            StringBuilder sb = new StringBuilder();
            sb.Append("RMS: " + obj.accountNumber);
            sb.Append("\n");

            if (obj.demographics != null)
            {
                sb.Append(obj.demographics.firstName + " " + obj.demographics.lastName);
                sb.Append("\n");

                if (obj.demographics.addresses[0] != null)
                {
                    sb.Append("Address: " + obj.demographics.addresses[0].addressLine1);
                    sb.Append("\n");
                    sb.Append("City: " + obj.demographics.addresses[0].city);
                    sb.Append("\n");
                    sb.Append("Zip Code: " + obj.customerDetails.countryCode);
                    sb.Append("\n");
                    sb.Append("birth Date: " + obj.demographics.birthDate);
                    sb.Append("\n");
                }
            }


            if (obj.employer != null)
            {
                sb.Append("Employer' name: " + obj.employer.name);
                sb.Append("\n");
                sb.Append("Employer' address: " + obj.employer.address);
                sb.Append("\n");
                sb.Append("Current Balance: " + obj.account.currentBalance);
                sb.Append("\n");
            }


            if (obj.financialSummary != null)
            {
                if (obj.financialSummary.accountAgingInMonths != null)
                {
                    sb.Append("Acct Aging in Month 01: " + obj.financialSummary.accountAgingInMonths.Month01);
                    sb.Append("\n");
                    sb.Append("Acct Aging in Month 02: " + obj.financialSummary.accountAgingInMonths.Month02);
                    sb.Append("\n");
                    sb.Append("Acct Aging in Month 03: " + obj.financialSummary.accountAgingInMonths.Month03);
                    sb.Append("\n");
                    sb.Append("Acct Aging in Month 04: " + obj.financialSummary.accountAgingInMonths.Month04);
                    sb.Append("\n");
                    sb.Append("Acct Aging in Month 05: " + obj.financialSummary.accountAgingInMonths.Month05);
                    sb.Append("\n");
                    sb.Append("Acct Aging in Month 06: " + obj.financialSummary.accountAgingInMonths.Month06);
                    sb.Append("\n");
                }

                if (obj.financialSummary.summaryCreditInMonths != null)
                {
                    sb.Append("Summary credit in Month 01: " + obj.financialSummary.summaryCreditInMonths.Month01);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 02: " + obj.financialSummary.summaryCreditInMonths.Month02);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 03: " + obj.financialSummary.summaryCreditInMonths.Month03);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 04: " + obj.financialSummary.summaryCreditInMonths.Month04);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 05: " + obj.financialSummary.summaryCreditInMonths.Month05);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 06: " + obj.financialSummary.summaryCreditInMonths.Month06);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 07: " + obj.financialSummary.summaryCreditInMonths.Month07);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 08: " + obj.financialSummary.summaryCreditInMonths.Month08);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 09: " + obj.financialSummary.summaryCreditInMonths.Month09);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 10: " + obj.financialSummary.summaryCreditInMonths.Month10);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 11: " + obj.financialSummary.summaryCreditInMonths.Month11);
                    sb.Append("\n");
                    sb.Append("Summary credit in Month 12: " + obj.financialSummary.summaryCreditInMonths.Month12);
                    sb.Append("\n");
                }

                if (obj.financialSummary.accountBalanceInMonths != null)
                {
                    sb.Append("Acct Balance in Month 01: " + obj.financialSummary.accountBalanceInMonths.Month01);
                    sb.Append("\n");
                    sb.Append("Acct Balance in Month 02: " + obj.financialSummary.accountBalanceInMonths.Month02);
                    sb.Append("\n");
                    sb.Append("Acct Balance in Month 03: " + obj.financialSummary.accountBalanceInMonths.Month03);
                    sb.Append("\n");
                    sb.Append("Acct Balance in Month 04: " + obj.financialSummary.accountBalanceInMonths.Month04);
                    sb.Append("\n");
                    sb.Append("Acct Balance in Month 05: " + obj.financialSummary.accountBalanceInMonths.Month05);
                    sb.Append("\n");
                    sb.Append("Acct Balance in Month 06: " + obj.financialSummary.accountBalanceInMonths.Month06);
                    sb.Append("\n");
                }

                if (obj.financialSummary.debitSummaryInMonths != null)
                {
                    sb.Append("Summary debits in Month01: " + obj.financialSummary.debitSummaryInMonths.Month01);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month02: " + obj.financialSummary.debitSummaryInMonths.Month02);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month03: " + obj.financialSummary.debitSummaryInMonths.Month03);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month04: " + obj.financialSummary.debitSummaryInMonths.Month04);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month05: " + obj.financialSummary.debitSummaryInMonths.Month05);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month06: " + obj.financialSummary.debitSummaryInMonths.Month06);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month07: " + obj.financialSummary.debitSummaryInMonths.Month07);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month08: " + obj.financialSummary.debitSummaryInMonths.Month08);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month09: " + obj.financialSummary.debitSummaryInMonths.Month09);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month10: " + obj.financialSummary.debitSummaryInMonths.Month10);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month11: " + obj.financialSummary.debitSummaryInMonths.Month11);
                    sb.Append("\n");
                    sb.Append("Summary debits in Month12: " + obj.financialSummary.debitSummaryInMonths.Month12);
                    sb.Append("\n");
                }

                if (obj.financialSummary.debitSummaryInMonthsUSD != null)
                {
                    sb.Append("Debit summary in Months USD: " + obj.financialSummary.debitSummaryInMonthsUSD);
                    sb.Append("\n");
                }
            }

            if (obj.customerFinancial != null)
            {
                sb.Append("CycleCut: " + obj.customerFinancial.cycleCut);
                sb.Append("\n");
                sb.Append("OtherIncome: " + obj.customerFinancial.otherIncome);
                sb.Append("\n");
                sb.Append("TotalPastdue: " + obj.customerFinancial.totalPastdue);
                sb.Append("\n");
            }

            #endregion

            string DATA = sb.ToString();
            WEBAPP.DataAccess.SKIPTRACE skiptrace = MapearItemSkipTrace("RMS" + obj.accountNumber, dtNow, dtNow.ToString("HH:mm:ss"), "PLCMNT", DATA);
            return skiptrace;
        }

        private static WEBAPP.DataAccess.SKIPTRACE CargarItemSkipTraceDesdeTreatments(dynamic obj, DateTime dtNow)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RMS: " + obj.accountNumber);
            sb.Append("\n");

            if (obj.treatments != null)
            {
                for (int i = 0; i < obj.treatments.Count; i++)
                {
                    sb.Append("updateDate: " + obj.treatments[i].updateDate);
                    sb.Append("\n");
                    sb.Append("accountCategoryTypeCode: " + obj.treatments[i].accountCategoryTypeCode);
                    sb.Append("\n");
                    sb.Append("treatmentEligibility: " + obj.treatments[i].treatmentEligibility);
                    sb.Append("\n");
                    sb.Append("treatmentProgram: " + obj.treatments[i].treatmentProgram);
                    sb.Append("\n");
                    sb.Append("agencyId: " + obj.treatments[i].agencyId);
                    sb.Append("\n");
                    sb.Append("accountInsertTimeStamp: " + obj.treatments[i].accountInsertTimeStamp);
                    sb.Append("\n");
                    sb.Append("isActive: " + obj.treatments[i].isActive);
                    sb.Append("\n");
                    sb.Append("------");

                }
            }



            string data = sb.ToString();
            WEBAPP.DataAccess.SKIPTRACE skiptrace = MapearItemSkipTrace("RMS" + obj.accountNumber, dtNow, dtNow.ToString("HH:mm:ss"), "PLCMNT", data);
            return skiptrace;
        }



        private static List<AMEX_INFO_ADIC_A> CargarItemListAmexInfoAdicA(dynamic obj, DateTime dtNow)
        {
            //FinancialTransactionDetails Key: accountNumber + accountCategoryTypeCode + transactionId
            List<AMEX_INFO_ADIC_A> listAmexInfoAdicA = new List<AMEX_INFO_ADIC_A>();

            if (obj.financialTransactionDetails != null)
            {
                for (int i = 0; i < obj.financialTransactionDetails.transactions.Count; i++)
                {
                    var updateDate = obj.financialTransactionDetails.transactions[i].updateDate;
                    var accountNumber = obj.financialTransactionDetails.accountNumber;
                    var currentForeignBalance = obj.financialTransactionDetails.transactions[i].currentForeignBalance;
                    var currentLocalCurrencyBalance = obj.financialTransactionDetails.transactions[i].currentLocalCurrencyBalance;
                    //var updatedBy = obj.financialTransactionDetails.transactions[i].updatedBy;
                    //var accountCategoryTypeCode = obj.financialTransactionDetails.transactions[i].accountCategoryTypeCode;
                    //var last5DigitsOfTransaction = obj.financialTransactionDetails.transactions[i].last5DigitsOfTransaction;
                    var transactionAmount = obj.financialTransactionDetails.transactions[i].transactionAmount;
                    var transactionTypeCode = obj.financialTransactionDetails.transactions[i].transactionTypeCode;
                    var transactionDate = obj.financialTransactionDetails.transactions[i].transactionDate;
                    //var legacyFieldCode = obj.financialTransactionDetails.transactions[i].legacyFieldCode;
                    var transactionCode = obj.financialTransactionDetails.transactions[i].transactionCode;
                    //var transactionId = obj.financialTransactionDetails.transactions[i].transactionId;
                    var currency = obj.financialTransactionDetails.transactions[i].currency == "ARS";
                    //var transactionReferId = obj.financialTransactionDetails.transactions[i].transactionReferId;
                    var foreignTransactionAmount = obj.financialTransactionDetails.transactions[i].foreignTransactionAmount;
                    //var foreignCurrencyCode = obj.financialTransactionDetails.transactions[i].foreignCurrencyCode;
                    //var stampedFxRate = obj.financialTransactionDetails.transactions[i].stampedFxRate;
                    //var sorTransactionCode = obj.financialTransactionDetails.transactions[i].sorTransactionCode;
                    //var acornPostDate = obj.financialTransactionDetails.transactions[i].acornPostDate;
                    //var processingStatus = obj.financialTransactionDetails.transactions[i].processingStatus;


                    //----------------------
                    if (transactionAmount != null)
                    {
                        //currentLocalCurrencyBalance
                        //transactionAmount
                        string tipo = "";
                        if (currentLocalCurrencyBalance != null)
                            tipo = currentLocalCurrencyBalance.ToString();

                        string monto = transactionAmount.ToString();

                        AMEX_INFO_ADIC_A amexInfoAdicAEnPesos = new AMEX_INFO_ADIC_A();
                        amexInfoAdicAEnPesos.IdMoroso = "RMS" + accountNumber;
                        amexInfoAdicAEnPesos.Fecha = Convert.ToDateTime(transactionDate);
                        amexInfoAdicAEnPesos.Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, true);
                        amexInfoAdicAEnPesos.Monto = transactionAmount;
                        amexInfoAdicAEnPesos.Descripcion = transactionTypeCode;

                        listAmexInfoAdicA.Add(amexInfoAdicAEnPesos);
                    }
                    if (foreignTransactionAmount != null)
                    {
                        //currentForeignBalance
                        //foreignTransactionAmount

                        string tipo = "";
                        if (currentLocalCurrencyBalance != null)
                            tipo = currentLocalCurrencyBalance.ToString();
                        string monto = "";
                        if (transactionAmount != null)
                            monto = transactionAmount.ToString();

                        AMEX_INFO_ADIC_A amexInfoAdicAEnDolares = new AMEX_INFO_ADIC_A();
                        amexInfoAdicAEnDolares.IdMoroso = "RMS" + accountNumber;
                        amexInfoAdicAEnDolares.Fecha = Convert.ToDateTime(transactionDate);
                        amexInfoAdicAEnDolares.Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, false);
                        amexInfoAdicAEnDolares.Monto = foreignTransactionAmount != null ? foreignTransactionAmount : 0;
                        amexInfoAdicAEnDolares.Descripcion = transactionTypeCode;

                        listAmexInfoAdicA.Add(amexInfoAdicAEnDolares);

                    }
                }
            }
            return listAmexInfoAdicA;
        }
        private static List<AMEX_LOG_DE_PAGO> CargarlistAmexLogDePago_desdeInfoAdicional(dynamic obj, DateTime dtNow)
        {
            //FinancialTransactionDetails Key: accountNumber + accountCategoryTypeCode + transactionId
            List<AMEX_LOG_DE_PAGO> listAmexLogDePago = new List<AMEX_LOG_DE_PAGO>();

            if (obj.financialTransactionDetails != null)
            {
                for (int i = 0; i < obj.financialTransactionDetails.transactions.Count; i++)
                {
                    var updateDate = obj.financialTransactionDetails.transactions[i].updateDate;
                    var accountNumber = obj.financialTransactionDetails.transactions[i].accountNumber;
                    var currentForeignBalance = obj.financialTransactionDetails.transactions[i].currentForeignBalance;
                    var currentLocalCurrencyBalance = obj.financialTransactionDetails.transactions[i].currentLocalCurrencyBalance;
                    //var updatedBy = obj.financialTransactionDetails.transactions[i].updatedBy;
                    //var accountCategoryTypeCode = obj.financialTransactionDetails.transactions[i].accountCategoryTypeCode;
                    //var last5DigitsOfTransaction = obj.financialTransactionDetails.transactions[i].last5DigitsOfTransaction;
                    var transactionAmount = obj.financialTransactionDetails.transactions[i].transactionAmount;
                    var transactionTypeCode = obj.financialTransactionDetails.transactions[i].transactionTypeCode;
                    var transactionDate = obj.financialTransactionDetails.transactions[i].transactionDate;
                    //var legacyFieldCode = obj.financialTransactionDetails.transactions[i].legacyFieldCode;
                    var transactionCode = obj.financialTransactionDetails.transactions[i].transactionCode;
                    //var transactionId = obj.financialTransactionDetails.transactions[i].transactionId;
                    var currency = obj.financialTransactionDetails.transactions[i].currency == "ARS";
                    //var transactionReferId = obj.financialTransactionDetails.transactions[i].transactionReferId;
                    var foreignTransactionAmount = obj.financialTransactionDetails.transactions[i].foreignTransactionAmount;

                    if (transactionAmount != null)
                    {
                        //currentLocalCurrencyBalance
                        //transactionAmount
                        string tipo = "";
                        if (currentLocalCurrencyBalance != null)
                            tipo = currentLocalCurrencyBalance.ToString();

                        string monto = transactionAmount.ToString();

                        listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
                        {
                            CargadoEn = dtNow,
                            IdMoroso = "RMS" + accountNumber,
                            Fecha = Convert.ToDateTime(transactionDate),
                            Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, true),
                            Monto = transactionAmount,
                            Descripcion = transactionTypeCode,
                            EsPesos = true
                        });
                    }
                    if (foreignTransactionAmount != null)
                    {
                        //currentForeignBalance
                        //foreignTransactionAmount

                        string tipo = "";
                        if (currentLocalCurrencyBalance != null)
                            tipo = currentLocalCurrencyBalance.ToString();
                        string monto = "";
                        if (transactionAmount != null)
                            monto = transactionAmount.ToString();

                        listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
                        {
                            CargadoEn = dtNow,
                            IdMoroso = "RMS" + accountNumber,
                            Fecha = Convert.ToDateTime(transactionDate),
                            Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, false),
                            Monto = foreignTransactionAmount != null ? foreignTransactionAmount : 0,
                            Descripcion = transactionTypeCode,
                            EsPesos = false
                        });
                    }
                }
            }
            return listAmexLogDePago;
        }
        private static List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO> CargarItemAmexPagosMinimo(dynamic obj, DateTime dtNow)
        {
            //AgencyInfo Key: agencyId
            //CustomerFinancial Key: accountNumber + accountCategoryTypeCode

            #region comentario
            //obj.customerFinancial.updateDate": "2018-09-10T20:41:00.486+0000",
            //obj.customerFinancial.updatedBy": "RMSI",
            //obj.customerFinancial.accountNumber": "376710729332007",
            //obj.customerFinancial.cycleCut": 3,
            //obj.customerFinancial.totalExposure": 9999.0,
            //obj.customerFinancial.unbilledAmountDue": 0.0,
            //obj.customerFinancial.currentAmountDue": 9.86,
            //obj.customerFinancial.amountDue150Days": 0.0
            //-------------------
            //amexPagosMinimo.Corriente = //??????
            #endregion

            List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO> listAmexPagosMinimo = new List<WEBAPP.DataAccess.AMEX_PAGOS_MINIMO>();

            WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnPesos = new WEBAPP.DataAccess.AMEX_PAGOS_MINIMO();
            amexPagosMinimoEnPesos.IdMoroso = "RMS" + obj.accountNumber;
            amexPagosMinimoEnPesos.IdCliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
            amexPagosMinimoEnPesos.Corriente = obj.customerFinancial.currentAmountDue != null ? obj.customerFinancial.currentAmountDue : 0;
            amexPagosMinimoEnPesos.PagoMinimo = obj.customerFinancial.minimumAmountDue != null ? obj.customerFinancial.minimumAmountDue : 0;// == null ? valorDefault : obj.customerFinancial.minimumAmountDue;
            amexPagosMinimoEnPesos.Dias30 = obj.customerFinancial.amountDue30Days != null ? obj.customerFinancial.amountDue30Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue30Days;
            amexPagosMinimoEnPesos.Dias60 = obj.customerFinancial.amountDue60Days != null ? obj.customerFinancial.amountDue60Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue60Days;
            amexPagosMinimoEnPesos.Dias90 = obj.customerFinancial.amountDue90Days != null ? obj.customerFinancial.amountDue90Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue90Days;
            amexPagosMinimoEnPesos.Dias120 = obj.customerFinancial.amountDue120Days != null ? obj.customerFinancial.amountDue120Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue120Days;
            amexPagosMinimoEnPesos.EsPesos = true;

            listAmexPagosMinimo.Add(amexPagosMinimoEnPesos);

            if (obj.customerFinancial.amountDue30DaysInUSD != null
                || obj.customerFinancial.amountDue60DaysInUSD != null
                || obj.customerFinancial.amountDue90DaysInUSD != null
                || obj.customerFinancial.amountDue120DaysInUSD != null
                || obj.customerFinancial.amountDue150DaysInUSD != null)
            {
                WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnDolar = new WEBAPP.DataAccess.AMEX_PAGOS_MINIMO();
                amexPagosMinimoEnDolar.IdMoroso = "RMS" + obj.accountNumber;
                amexPagosMinimoEnDolar.IdCliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
                amexPagosMinimoEnDolar.Corriente = obj.customerFinancial.currentAmountDueInUSD != null ? obj.customerFinancial.currentAmountDueInUSD : 0;
                amexPagosMinimoEnDolar.PagoMinimo = obj.customerFinancial.totalAmountDueInUSD != null ? obj.customerFinancial.totalAmountDueInUSD : 0;// == null ? valorDefault : obj.customerFinancial.minimumAmountDue;
                amexPagosMinimoEnDolar.Dias30 = obj.customerFinancial.amountDue30DaysInUSD != null ? obj.customerFinancial.amountDue30DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue30Days;
                amexPagosMinimoEnDolar.Dias60 = obj.customerFinancial.amountDue60DaysInUSD != null ? obj.customerFinancial.amountDue60DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue60Days;
                amexPagosMinimoEnDolar.Dias90 = obj.customerFinancial.amountDue90DaysInUSD != null ? obj.customerFinancial.amountDue90DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue90Days;
                amexPagosMinimoEnDolar.Dias120 = obj.customerFinancial.amountDue120DaysInUSD != null ? obj.customerFinancial.amountDue120DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue120Days;
                amexPagosMinimoEnDolar.EsPesos = false;

                listAmexPagosMinimo.Add(amexPagosMinimoEnDolar);
            }

            try
            {
                RegistrarPrimaryKeyHelper.RegistrarPrimaryKey(PrimaryKeyFor: "CustomerFinancial", AccountNumber: obj.accountNumber, AccountCategoryTypeCode: obj.AccountCategoryTypeCode);
            }
            catch (Exception ex) { }

            return listAmexPagosMinimo;
        }
        private static List<AMEX_LOG_DE_PAGO> CargarlistAmexLogDePago_desdePagosMinimo(dynamic obj, DateTime dtNow)
        {
            //FinancialTransactionDetails Key: accountNumber + accountCategoryTypeCode + transactionId

            List<AMEX_LOG_DE_PAGO> listAmexLogDePago = new List<AMEX_LOG_DE_PAGO>();

            //var transactionDate = obj.financialTransactionDetails.transactions[i].transactionDate;
            //var transactionTypeCode = obj.financialTransactionDetails.transactions[i].transactionTypeCode;
            //var transactionCode = obj.financialTransactionDetails.transactions[i].transactionCode;
            //var transactionAmount = obj.financialTransactionDetails.transactions[i].transactionAmount;
            //var currentLocalCurrencyBalance = obj.financialTransactionDetails.transactions[i].currentLocalCurrencyBalance;
            //var foreignTransactionAmount = obj.financialTransactionDetails.transactions[i].foreignTransactionAmount;

            //string tipo = "";
            //if (currentLocalCurrencyBalance != null)
            //    tipo = currentLocalCurrencyBalance.ToString();
            //
            //string monto = transactionAmount.ToString();

            //WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnPesos = new WEBAPP.DataAccess.AMEX_PAGOS_MINIMO();
            //amexPagosMinimoEnPesos.IdCliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
            //amexPagosMinimoEnPesos.Corriente = obj.customerFinancial.currentAmountDue != null ? obj.customerFinancial.currentAmountDue : 0;
            //amexPagosMinimoEnPesos.PagoMinimo = obj.customerFinancial.minimumAmountDue != null ? obj.customerFinancial.minimumAmountDue : 0;// == null ? valorDefault : obj.customerFinancial.minimumAmountDue;
            //amexPagosMinimoEnPesos.Dias30 = obj.customerFinancial.amountDue30Days != null ? obj.customerFinancial.amountDue30Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue30Days;
            //amexPagosMinimoEnPesos.Dias60 = obj.customerFinancial.amountDue60Days != null ? obj.customerFinancial.amountDue60Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue60Days;
            //amexPagosMinimoEnPesos.Dias90 = obj.customerFinancial.amountDue90Days != null ? obj.customerFinancial.amountDue90Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue90Days;
            //amexPagosMinimoEnPesos.Dias120 = obj.customerFinancial.amountDue120Days != null ? obj.customerFinancial.amountDue120Days : 0;// == null ? valorDefault : obj.customerFinancial.amountDue120Days;
            //amexPagosMinimoEnPesos.EsPesos = true;

            //listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
            //{
            //    CargadoEn = dtNow,
            //    IdMoroso = "RMS" + obj.accountNumber,
            //    Fecha = Convert.ToDateTime(transactionDate),
            //    Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, true),
            //    Monto = transactionAmount,
            //    Descripcion = transactionTypeCode,
            //    EsPesos = true
            //});



            //if (obj.customerFinancial.amountDue30DaysInUSD != null || obj.customerFinancial.amountDue60DaysInUSD != null || obj.customerFinancial.amountDue90DaysInUSD != null || obj.customerFinancial.amountDue120DaysInUSD != null || obj.customerFinancial.amountDue150DaysInUSD != null)
            //{
            //  WEBAPP.DataAccess.AMEX_PAGOS_MINIMO amexPagosMinimoEnDolar = new WEBAPP.DataAccess.AMEX_PAGOS_MINIMO();
            //  amexPagosMinimoEnDolar.IdMoroso = "RMS" + obj.accountNumber;
            //  amexPagosMinimoEnDolar.IdCliente = "AMEX_" + obj.agencyInfo.legacyRecoverCode;
            //  amexPagosMinimoEnDolar.Corriente = obj.customerFinancial.currentAmountDueInUSD != null ? obj.customerFinancial.currentAmountDueInUSD : 0;
            //  amexPagosMinimoEnDolar.PagoMinimo = obj.customerFinancial.totalAmountDueInUSD != null ? obj.customerFinancial.totalAmountDueInUSD : 0;// == null ? valorDefault : obj.customerFinancial.minimumAmountDue;
            //  amexPagosMinimoEnDolar.Dias30 = obj.customerFinancial.amountDue30DaysInUSD != null ? obj.customerFinancial.amountDue30DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue30Days;
            //  amexPagosMinimoEnDolar.Dias60 = obj.customerFinancial.amountDue60DaysInUSD != null ? obj.customerFinancial.amountDue60DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue60Days;
            //  amexPagosMinimoEnDolar.Dias90 = obj.customerFinancial.amountDue90DaysInUSD != null ? obj.customerFinancial.amountDue90DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue90Days;
            //  amexPagosMinimoEnDolar.Dias120 = obj.customerFinancial.amountDue120DaysInUSD != null ? obj.customerFinancial.amountDue120DaysInUSD : 0;// == null ? valorDefault : obj.customerFinancial.amountDue120Days;
            //  amexPagosMinimoEnDolar.EsPesos = false;

            //listAmexLogDePago.Add(new AMEX_LOG_DE_PAGO()
            //    {
            //        IdMoroso = "RMS" + obj.accountNumber,
            //        Fecha = Convert.ToDateTime(transactionDate),
            //        Tipo = ObtenerTipoDePayment(transactionTypeCode, transactionCode, tipo, monto, false),
            //        Monto = foreignTransactionAmount != null ? foreignTransactionAmount : 0,
            //        Descripcion = transactionTypeCode,
            //        EsPesos = false
            //    });

            //}

            return listAmexLogDePago;

        }
        #endregion

        #region Mapeado
        private static DataAccess.SKIPTRACE MapearItemSkipTrace(string DEBTORID, DateTime datetime, string time, string ORIGIN, string DATA)
        {
            WEBAPP.DataAccess.SKIPTRACE skiptrace = new WEBAPP.DataAccess.SKIPTRACE();
            skiptrace.DEBTORID = DEBTORID;
            skiptrace.DATE = datetime;
            skiptrace.TIME = time;
            skiptrace.ORIGIN = ORIGIN;
            skiptrace.DATA = DATA;
            return skiptrace;
        }


        //private string ObtenerTipoDePayment(string transactionTypeCode, string transactionCode, string currentLocalCurrencyBalance, string transactionAmount, bool montoEnPesos)
        private static string ObtenerTipoDePayment(string transactionTypeCode, string transactionCode, string currentLocalCurrencyBalance, string transactionAmount, bool montoEnPesos)
        {
            string result = "";

            if (transactionTypeCode == "payment")
            {
                if (transactionCode == "11")
                {
                    result = "Débito / Ajuste";
                }
                else if (transactionCode == "12")
                {
                    result = "Crédito";
                }
                else if (transactionCode == "50")
                {
                    result = "Pago";
                }
                else if (transactionCode == "52")
                {
                    result = "Pago reverso";
                }
            }
            else
            {
                if (montoEnPesos)
                {
                    if (!string.IsNullOrEmpty(transactionAmount))
                    {
                        result = "monto de pago en moneda local";
                    }
                    else if (!string.IsNullOrEmpty(currentLocalCurrencyBalance))
                    {
                        result = "balance en moneda local";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(transactionAmount))
                    {
                        result = "pago en US Dollars";
                    }
                    else if (!string.IsNullOrEmpty(currentLocalCurrencyBalance))
                    {
                        result = "balance en US Dollars";
                    }
                }
            }

            return result;
        }

        private static string ObtenerTipoFromDescripcion(string currentLocalCurrencyBalance, string transactionAmount, bool montoEnPesos)
        {
            string result = "";

            if (montoEnPesos)
            {
                if (!string.IsNullOrEmpty(transactionAmount))
                {
                    result = "monto de pago en moneda local";
                }
                else if (!string.IsNullOrEmpty(currentLocalCurrencyBalance))
                {
                    result = "balance en moneda local";
                }

                //switch (Descripcion.ToUpper().Trim())
                //{
                //    case "CREDIT ADJUSTMENT -":
                //        Console.WriteLine("Ajustes de Credito");
                //        break;
                //    case "RECONCILIATION ADJ":
                //        Console.WriteLine("Ajuste de Reconcializacion");
                //        break;
                //    case "BALANCE IN LOCAL CUR":
                //        Console.WriteLine("balance en moneda local");
                //        break;
                //    case "PAYMENT":
                //        Console.WriteLine("monto de pago en moneda local");
                //        break;
                //    case "NSF DIRECT":
                //        Console.WriteLine("Sin Fondos Suficientes NSF");
                //        break;
                //    case "DEBIT ADJUSTMENT - O":
                //        Console.WriteLine("Ajustes de Debito");
                //        break;
                //}
            }
            else
            {
                if (!string.IsNullOrEmpty(transactionAmount))
                {
                    result = "pago en US Dollars";
                }
                else if (!string.IsNullOrEmpty(currentLocalCurrencyBalance))
                {
                    result = "balance en US Dollars";
                }
                //switch (Descripcion.ToUpper().Trim())
                //{
                //    case "PAYMENT":
                //        Console.WriteLine("pago en US Dollars");
                //        break;
                //    case "BALANCE IN USD":
                //        Console.WriteLine("balance en US Dollars");
                //        break;
                //}
            }
            //switch (Descripcion.ToUpper().Trim())
            //{
            //    case "CREDIT ADJUSTMENT -":
            //        Console.WriteLine("Ajustes de Credito");
            //        break;
            //    case "PAYMENT IN USD":
            //        Console.WriteLine("pago en US Dollars");
            //        break;
            //    case "RECONCILIATION ADJ":
            //        Console.WriteLine("Ajuste de Reconcializacion");
            //        break;
            //    case "BALANCE IN USD":
            //        Console.WriteLine("balance en US Dollars");
            //        break;
            //    case "BALANCE IN LOCAL CUR":
            //        Console.WriteLine("balance en moneda local");
            //        break;
            //    case "PAYMENT IN LOCAL CUR":
            //        Console.WriteLine("monto de pago en moneda local");
            //        break;
            //    case "NSF DIRECT":
            //        Console.WriteLine("Sin Fondos Suficientes NSF");
            //        break;
            //    case "DEBIT ADJUSTMENT - O":
            //        Console.WriteLine("Ajustes de Debito");
            //        break;
            //}

            return result;
        }

        private string MapearActivityTypeCode(string ActivityTypeCode)
        {
            string result = "";
            switch (ActivityTypeCode)
            {
                case "AF":
                    result = "Add Strategy Indicator";
                    break;
                case "AP":
                    result = "Strategy I- ACCT Purge";
                    break;
                case "AR":
                    result = "Application Request";
                    break;
                case "AS":
                    result = "Add CAS Support";
                    break;
                case "AU":
                    result = "Auto Pay Indicator";
                    break;
                case "AW":
                    result = "Corporate Rep Portal";
                    break;
                case "A1":
                    result = "Notational 1";
                    break;
                case "A2":
                    result = "Notational 2";
                    break;
                case "A3":
                    result = "Notational 3";
                    break;
                case "A4":
                    result = "Notational 4";
                    break;
                case "A5":
                    result = "Notational 5";
                    break;
                case "BA":
                    result = "Bankruptcy";
                    break;
                case "BC":
                    result = "Bank Check Request";
                    break;
                case "BL":
                    result = "Budgetary Limit";
                    break;
                case "BP":
                    result = "Broken Promise";
                    break;
                case "CA":
                    result = "Credit Assist";
                    break;
                case "CB":
                    result = "Credit Bureaau";
                    break;
                case "CC":
                    result = "Cancellation Complete";
                    break;
                case "CE":
                    result = "CAS Error";
                    break;
                case "CF":
                    result = "Cancel Flex";
                    break;
                case "CH":
                    result = "Click to Chat";
                    break;
                case "CI":
                    result = "CM COOP Ind";
                    break;
                case "CL":
                    result = "CCSG Lending  ORT (Override Review Trigger)";
                    break;
                case "CM":
                    result = "Compliance";
                    break;
                case "CO":
                    result = "CCSG Charge ORT (Override Review Trigger)";
                    break;
                case "CP":
                    result = "ACS - Call Process";
                    break;
                case "CR":
                    result = "Cancellation Request";
                    break;
                case "CU":
                    result = "CAS Update";
                    break;
                case "CW":
                    result = "Credit Worth Rank";
                    break;
                case "DA":
                    result = "Deceased Account";
                    break;
                case "DC":
                    result = "Delete CAS Negative Code";
                    break;
                case "DE":
                    result = "Dummy Entry";
                    break;
                case "DF":
                    result = "Delete Strategy Indicator";
                    break;
                case "DI":
                    result = "Delink Inactive";
                    break;
                case "DL":
                    result = "Decrease Line";
                    break;
                case "DM":
                    result = "PDSO SMS Alert";
                    break;
                case "DN":
                    result = "Default Notice";
                    break;
                case "DP":
                    result = "Delete Promise";
                    break;
                case "DR":
                    result = "Disclosure Read";
                    break;
                case "DS":
                    result = "Delete CAS Support";
                    break;
                case "DU":
                    result = "Defer Eligibility Update";
                    break;
                case "ED":
                    result = "Email Denied";
                    break;
                case "EN":
                    result = "Entered CC90's";
                    break;
                case "EP":
                    result = "Electronic Payment";
                    break;
                case "ES":
                    result = "Email sent";
                    break;
                case "FM":
                    result = "Film Request";
                    break;
                case "FR":
                    result = "Fraud Research";
                    break;
                case "FS":
                    result = "Fraud Scanner";
                    break;
                case "FX":
                    result = "Faxed";
                    break;
                case "GD":
                    result = "General Disclosure";
                    break;
                case "GI":
                    result = "Global Limit Income";
                    break;
                case "GL":
                    result = "Permanent Global Limitation";
                    break;
                case "GP":
                    result = "Strategy I- GEN Purge";
                    break;
                case "GR":
                    result = "Global Limit Removed";
                    break;
                case "IC":
                    result = "Incoming Call";
                    break;
                case "ID":
                    result = "Fraud Override ID";
                    break;
                case "II":
                    result = "Initial Information";
                    break;
                case "IL":
                    result = "Increase Line";
                    break;
                case "IN":
                    result = "ICN Notation";
                    break;
                case "KP":
                    result = "Kept Promise";
                    break;
                case "LC":
                    result = "Location Change";
                    break;
                case "LD":
                    result = "Letter Delete";
                    break;
                case "LN":
                    result = "Letter Notation";
                    break;
                case "LR":
                    result = "Letter Received";
                    break;
                case "LS":
                    result = "Letter Sent";
                    break;
                case "MA":
                    result = "Merchant Action";
                    break;
                case "MD":
                    result = "Control Delete CAS Negative Code";
                    break;
                case "MN":
                    result = "Multiple Lines";
                    break;
                case "MP":
                    result = "Control Add CAS Negative Code";
                    break;
                case "NG":
                    result = "Check TRAN";
                    break;
                case "NN":
                    result = "Check Notes";
                    break;
                case "NP":
                    result = "Check Paid";
                    break;
                case "NR":
                    result = "Check Retry";
                    break;
                case "OA":
                    result = "Outside Agency";
                    break;
                case "OC":
                    result = "Outgoing Call";
                    break;
                case "OF":
                    result = "OPEN Feature ORT (Override Review Trigger)";
                    break;
                case "OL":
                    result = "OPEN Lending ORT (Override Review Trigger)";
                    break;
                case "OO":
                    result = "OPEN Charge ORT (Override Review Trigger)";
                    break;
                case "OS":
                    result = "OA Status";
                    break;
                case "OT":
                    result = "Offer Tool";
                    break;
                case "PA":
                    result = "Portfolio Assignment";
                    break;
                case "PC":
                    result = "Portfolio Change";
                    break;
                case "PD":
                    result = "Delete PAR";
                    break;
                case "PE":
                    result = "Promise Email";
                    break;
                case "PF":
                    result = "Portfolio Transfer";
                    break;

                case "PL":
                    result = "Promise Letter";
                    break;
                case "PM":
                    result = "Take ORCC Promise";
                    break;
                case "PN":
                    result = "Propagated Notes";
                    break;
                case "PR":
                    result = "Anticipated Spend";
                    break;
                case "PS":
                    result = "Portfolio Reassign";
                    break;
                case "PT":
                    result = "Take Promise";
                    break;
                case "PU":
                    result = "Change ORCC Promise";
                    break;
                case "PV":
                    result = "Add PAR";
                    break;
                case "PX":
                    result = "Change Promise";
                    break;
                case "PY":
                    result = "Payment Received";
                    break;
                case "QC":
                    result = "Quality Check";
                    break;
                case "RA":
                    result = "Remittamce Aware";
                    break;
                case "RC":
                    result = "Review Callback";
                    break;
                case "RD":
                    result = "Reinstate Decision";
                    break;
                case "RF":
                    result = "Reactivate Flex";
                    break;
                case "RH":
                    result = "Review High Balance";
                    break;
                case "RI":
                    result = "Route Inactive";
                    break;
                case "RL":
                    result = "Route to Laser";
                    break;
                case "RM":
                    result = "Remedy Cancel";
                    break;
                case "RO":
                    result = "Talk Off Accepted";
                    break;
                case "RR":
                    result = "Route for Review";
                    break;
                case "RV":
                    result = "Review";
                    break;
                case "SC":
                    result = "Status CD Update";
                    break;
                case "SF":
                    result = "Suspend Flex";
                    break;
                case "SI":
                    result = "Sent Inactive";
                    break;
                case "SP":
                    result = "Significant Even Process";
                    break;
                case "SR":
                    result = "Suspense Release";
                    break;
                case "ST":
                    result = "Settlement Request";
                    break;
                case "SV":
                    result = "Suspected Victim";
                    break;
                case "TC":
                    result = "Total Customer Charge";
                    break;
                case "TL":
                    result = "Total Customer Lending";
                    break;
                case "TM":
                    result = "Test Message Requested";
                    break;
                case "TR":
                    result = "Term Requested";
                    break;
                case "TS":
                    result = "Telegram Sent";
                    break;
                case "TU":
                    result = "Text Message Fail";
                    break;
                case "TV":
                    result = "Text Message Cancelled";
                    break;
                case "TW":
                    result = "Text Message Rejected";
                    break;
                case "UC":
                    result = "Add CAS Negative Status Code";
                    break;
                case "VP":
                    result = "ACS - Call For West";
                    break;
                case "WN":
                    result = "We Need";
                    break;
                case "WP":
                    result = "Web Promise";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }

        private string MapearContactTypeCode(string ContactTypeCode)
        {
            string result = "";
            switch (ContactTypeCode)
            {
                case "B":
                    result = "Basic";
                    break;
                case "C":
                    result = "Accountant";
                    break;
                case "F":
                    result = "Friend / Unauthorized spouse";
                    break;
                case "I":
                    result = "Disconnects";
                    break;
                case "L":
                    result = "LMTC [Left message to call back (to a person)]";
                    break;
                case "M":
                    result = "Answering machine (Left message)";
                    break;
                case "N":
                    result = "No answer";
                    break;
                case "O":
                    result = "Other";
                    break;
                case "P":
                    result = "Authorized third party";
                    break;
                case "R":
                    result = "Other relative";
                    break;
                case "S":
                    result = "Supp";
                    break;
                case "T":
                    result = "Attorney";
                    break;
                case "U":
                    result = "Unlisted";
                    break;
                case "W":
                    result = "Wrong party";
                    break;
                case "Y":
                    result = "Busy";
                    break;
                case "Z":
                    result = "CCCS (Credit Consumer Counseling Servicing someone acting on Card Member's behalf, used for bank ref)";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }

        private string MapearPartyContactCode(string PartyContactCode)
        {
            string result = "";
            switch (PartyContactCode)
            {
                case "A":
                    result = "Corporate Sales Contact";
                    break;
                case "B":
                    result = "Basic Cardmember";
                    break;
                case "C":
                    result = "Accountant";
                    break;
                case "D":
                    result = "Corporate Officer";
                    break;
                case "E":
                    result = "Not Employed";
                    break;
                case "F":
                    result = "Other Friend/Unauthorised Spouse";
                    break;
                case "G":
                    result = "Valid Home";
                    break;
                case "H":
                    result = "Private Number";
                    break;
                case "I":
                    result = "Disconnect";
                    break;
                case "J":
                    result = "Valid Business";
                    break;
                case "K":
                    result = "Corporate Contact";
                    break;
                case "L":
                    result = "Left Message To Call Back";
                    break;
                case "M":
                    result = "Answering Machine";
                    break;
                case "N":
                    result = "No Answer";
                    break;
                case "O":
                    result = "Other";
                    break;
                case "P":
                    result = "Authorised Spouse";
                    break;
                case "Q":
                    result = "Other Creditor";
                    break;
                case "R":
                    result = "Other Relative";
                    break;
                case "S":
                    result = "Supp Cardmember";
                    break;
                case "T":
                    result = "Attorney";
                    break;
                case "U":
                    result = "Unlisted";
                    break;
                case "V":
                    result = "Verify";
                    break;
                case "W":
                    result = "Right Number, Wrong Party";
                    break;
                case "Z":
                    result = "Other Confirmed Credit Contact (CCC)";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }

        private string MapearPlaceContactCode(string PlaceContactCode)
        {
            string result = "";
            switch (PlaceContactCode)
            {
                case "B":
                    result = "Basic Business";
                    break;
                case "C":
                    result = "Consumer Credit Counseling (CCCS)";
                    break;
                case "D":
                    result = "Delete All";
                    break;
                case "E":
                    result = "Service Establishment";
                    break;
                case "F":
                    result = "Fraud Scanner (initial case set-up)";
                    break;
                case "H":
                    result = "Basic Home";
                    break;
                case "I":
                    result = "Information Operator";
                    break;
                case "K":
                    result = "Bank";
                    break;
                case "N":
                    result = "Dunn & Bradstreet";
                    break;
                case "O":
                    result = "Other";
                    break;
                case "P":
                    result = "Supp Business";
                    break;
                case "S":
                    result = "Supp Home";
                    break;
                case "Z":
                    result = "Authorized Third Party";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }

        private string MapearPromiseFrequencyCode(string PromiseFrequencyCode)
        {
            string result = "";
            switch (PromiseFrequencyCode)
            {
                case "Weekly - W0":
                    result = "Promises scheduled weekly";
                    break;
                case "Bi_Weekly - B0":
                    result = "Promises scheduled every two-weeks";
                    break;
                case "Monthly - M0":
                    result = "Promises scheduled monthly";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }

        private string MapearPromisePaymentMethod(string PromisePaymentMethod)
        {
            string result = "";
            switch (PromisePaymentMethod)
            {
                case "Paper":
                    result = "these may include: Money Orders, Checks, Balance transfer checks that are mailed, bill pay that are mailed";
                    break;
                case "Other":
                    result = "these may include: Pay by Phone/IVR including debt Cards, Payments made on MYCA or Mobile App, Bill Pay made electronically, balance transfer made electronically and wire transfers.";
                    break;
                default:
                    Console.WriteLine("");
                    break;
            }
            return result;
        }
        #endregion

        private void MostrardivLbError(bool huboError, string msj, System.Web.UI.HtmlControls.HtmlGenericControl divRowLbError, System.Web.UI.HtmlControls.HtmlGenericControl divLbError, Label lbError, bool visible = true)
        {
            divRowLbError.Visible = visible;
            divLbError.Attributes.Add("class", huboError ? "alert alert-danger" : "alert alert-success");
            lbError.Text = msj;
        }

        private void CrearArchivoDeRecepcion(string fileName, string pathData, string tipo, string reportId)
        {
            string relativePath = VirtualPathUtility.ToAbsolute("~/File/AMEX_ACORN/aceptacion.json");
            string path = Server.MapPath(relativePath);

            string hash = "";

            #region Nombre del Archivo
            string[] split = fileName.Split('.');
            string a = split[0];
            string b = split[1];
            string c = split[2];
            string d = split[3];
            string version = split[4];
            string fecha = split[5];
            string secuencia = split[6];

            string nombreDelArchivo = string.Format("LZ.MO.UNITECH.{0}.{1}.{2}.{3}", tipo, version, fecha, secuencia);
            #endregion

            #region Contenido del Archivos
            System.IO.StreamReader file = new System.IO.StreamReader(pathData);
            string data = file.ReadToEnd();
            file.Close();
            hash = HashUtils.ComputeSha256Hash(data);
            StringBuilder sb = new StringBuilder();
            sb.Append(reportId.PadRight(15));
            sb.Append(secuencia.PadLeft(8, '0') + hash);
            string contenidoDeArchivo = sb.ToString();
            #endregion

            File.WriteAllText(path, contenidoDeArchivo);

            System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.Open);
            byte[] btFile = new byte[fs.Length];
            fs.Read(btFile, 0, Convert.ToInt32(fs.Length));
            fs.Close();
            Response.AddHeader("Content-disposition", "attachment; filename=" + nombreDelArchivo);
            Response.ContentType = "application/octet-stream";
            Response.BinaryWrite(btFile);
            Response.End();
        }

        public override string BackURL
        {
            get
            {
                return "Applications/DYNAMIC/AMEX/AMEX_ACORN/Default.aspx";
            }
        }


    }
}