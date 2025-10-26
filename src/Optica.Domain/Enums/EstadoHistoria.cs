using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Enums
{
    public enum EstadoHistoria
    {
        Creada = 0,
        Registrada = 1,
        ListaParaEnvio = 2,
        EnTransitoASucursal = 3,
        RecibidaEnSucursal = 4,
        EnviadaALaboratorio = 5,
        ListaEnLaboratorio = 6,
        RecibidaEnSucursalCentral = 7,
        ListaParaEntrega = 8,
        RecibidaEnSucursalOrigen = 9,
        EntregadaAlCliente = 10
    }

    public enum Ojo { OD = 0, OI = 1 }

    public enum CondicionAV { SinLentes = 0, ConLentes = 1 }

    //public enum RxDistancia { Lejos = 0, Cerca = 1 }


    //public enum MetodoPago { Efectivo = 0, Tarjeta = 1 }

    public enum TipoLenteContacto { Esferico = 0, Torico = 1, Otro = 2 }

}
