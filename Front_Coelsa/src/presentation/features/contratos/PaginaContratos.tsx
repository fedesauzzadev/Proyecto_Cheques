// Pestaña "Contratos" (RF-F09): Swagger UI de la API embebido en un iframe,
// sin salir de la consola. La URL deriva de la misma base que usa el cliente
// HTTP (en prod apunta a la API prod, en dev a la API dev).
import { obtenerUrlBaseApi } from '@/infrastructure/clienteHttp';

export default function PaginaContratos() {
  const urlSwagger = `${obtenerUrlBaseApi()}/swagger/index.html`;

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Contratos de la API</h2>
        <p className="text-sm text-muted-foreground">
          Documentación interactiva (Swagger UI) servida por la propia API.
        </p>
      </div>
      <iframe
        title="Documentación Swagger de la API COELSA"
        src={urlSwagger}
        className="h-[calc(100vh-14rem)] min-h-[480px] w-full rounded-md border bg-white"
      />
    </div>
  );
}
