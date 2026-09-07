// Valores iniciales del formulario: moneda en pesos, carácter a la orden,
// tipo de documento CUIT, emisión hoy, resto vacío.
export function valoresIniciales(nombres: readonly string[]): Record<string, string> {
  const hoy = new Date().toISOString().slice(0, 10);
  const valores: Record<string, string> = {};
  for (const nombre of nombres) {
    valores[nombre] =
      nombre === 'moneda'
        ? 'P'
        : nombre === 'caracter'
          ? 'AlaOrden'
          : nombre === 'tipoDocBeneficiario'
            ? 'CUIT'
            : nombre === 'fechaEmision'
              ? hoy
              : '';
  }
  return valores;
}
