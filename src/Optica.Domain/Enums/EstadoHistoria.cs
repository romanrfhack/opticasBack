using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Enums
{
    public enum EstadoHistoria
    {
        Borrador = 0,
        Guardado = 1,
        EnLaboratorio = 2,
        EnSucursal = 3,
        Entregado = 4,
        Cerrado = 5
    }

    public enum Ojo { OD = 0, OI = 1 }

    public enum CondicionAV { SinLentes = 0, ConLentes = 1 }

    //public enum RxDistancia { Lejos = 0, Cerca = 1 }


    //public enum MetodoPago { Efectivo = 0, Tarjeta = 1 }

    public enum TipoLenteContacto { Esferico = 0, Torico = 1, Otro = 2 }

}
