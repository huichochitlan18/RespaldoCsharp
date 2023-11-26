using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resplado.Models;

public class Ruta
{
    public string Origen { get; set; } = String.Empty;
    public string Destino{ get; set; } = String.Empty;
    public int Tipo { get; set; }
}

