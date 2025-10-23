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
        EnTransitoASucursal = 2,
        RecibidaEnSucursal = 3,
        EnviadaALaboratorio = 4,
        ListaEnLaboratorio = 5,
        RecibidaEnSucursalCentral = 6,
        ListaParaEntrega = 7,
        RecibidaEnSucursalOrigen = 8,
        EntregadaAlCliente = 9
    }

    public enum Ojo { OD = 0, OI = 1 }

    public enum CondicionAV { SinLentes = 0, ConLentes = 1 }

    //public enum RxDistancia { Lejos = 0, Cerca = 1 }


    //public enum MetodoPago { Efectivo = 0, Tarjeta = 1 }

    public enum TipoLenteContacto { Esferico = 0, Torico = 1, Otro = 2 }

}
