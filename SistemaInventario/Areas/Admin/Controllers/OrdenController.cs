using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using System.Security.Claims;

namespace SistemaInventario.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrdenController : Controller
    {
        private readonly IUnidadTrabajo _unidadTrabajo;

        public OrdenDetalleVM ordenDetalleVM { get; set; }

        public OrdenController(IUnidadTrabajo unidadTrabajo)
        {
            _unidadTrabajo = unidadTrabajo;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Detalle(int id)
        {
            ordenDetalleVM = new OrdenDetalleVM()
            {
                Orden = await _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id, incluirPropiedades: "UsuarioAplicacion"),
                OrdenDetalle = await _unidadTrabajo.OrdenDetalle.ObtenerTodos(d => d.OrdenId == id, incluirPropiedades: "Producto")
            };
            return View(ordenDetalleVM);
        }

        [Authorize(Roles = DS.Role_Admin)]
        public async Task<IActionResult> Procesar(int id)
        {
            var orden = await _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id);
            orden.EstadoOrden = DS.EstadoEnProceso;
            await _unidadTrabajo.Guardar();
            TempData[DS.Exitosa] = "Orden cambiada a Estado en proceso";
            return RedirectToAction("Detalle", new { id = id});
        }

        [HttpPost]
        [Authorize(Roles = DS.Role_Admin)]
        public async Task<IActionResult> EnviarOrden(OrdenDetalleVM ordenDetalleVM)
        {
            var orden = await _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == ordenDetalleVM.Orden.Id);
            orden.EstadoOrden = DS.EstadoEnviado;
            orden.Carrier = ordenDetalleVM.Orden.Carrier;
            orden.NumeroEnvio = ordenDetalleVM.Orden.NumeroEnvio;
            orden.FechaEnvio = DateTime.Now;
            await _unidadTrabajo.Guardar();
            TempData[DS.Exitosa] = "Orden cambiada a Estado enviado";
            return RedirectToAction("Detalle", new { id = ordenDetalleVM.Orden.Id });
        }
        #region
        [HttpGet]
        public async Task<IActionResult> ObtenerOrdenLista(string estado) {
            var claimIdentidad = (ClaimsIdentity)User.Identity;
            var claim = claimIdentidad.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<Orden> todos;

            if (User.IsInRole(DS.Role_Admin)) {
                todos = await _unidadTrabajo.Orden.ObtenerTodos(incluirPropiedades: "UsuarioAplicacion");
            }
            else { 
                todos = await _unidadTrabajo.Orden.ObtenerTodos(o=> o.UsuarioAplicacionId == claim.Value, incluirPropiedades: "UsuarioAplicacion");
            
            }

            switch (estado){
                case "aprobado":
                    todos = todos.Where(o => o.EstadoOrden == DS.EstadoAprobado);
                    break;
                case "completado":
                    todos = todos.Where(o => o.EstadoOrden == DS.EstadoEnviado);
                    break;
                default:
                    break;
            }
            return Json(new { data = todos });
        }
        #endregion
    }
}
