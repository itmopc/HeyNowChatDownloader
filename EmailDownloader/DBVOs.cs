using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailDownloader
{
    public class HeyNowChatsHeaderVO
    {
        public string SessionId { get; set; }
        public string IdMoroso { get; set; }
        public string IdCliente { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaInicioSesion { get; set; }
        public DateTime FechaFinSesion { get; set; }
        public string NombreAgente { get; set; }

        public dynamic NombreMoroso { get; set; }

        public string IdAgente { get; set; }

        public string IDOPERADOR { get; set; }
        public DateTime? FIRSTAGENTCONTACT { get; set; }
        public DateTime? CONTACTLASTINTERACTIONDATE { get; set; }

        public bool derivadoAOperador { get; set; }


        public List<HeyNowChatsItemVO> ListaItems { get; set; }

        public HeyNowChatsHeaderVO()
        {
            ListaItems = new List<HeyNowChatsItemVO>();
        }
    }

    public class HeyNowChatsItemVO
    {
        public string Id { get; set; }
        public DateTime? Fecha { get; set; }
        public bool Incoming { get; set; }
        public string Mensaje { get; set; }
        public string IdAgente { get; set; }

    }
}
