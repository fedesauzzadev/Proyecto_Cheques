// Tests de la pestaña Contratos (RF-F09): el Swagger queda embebido con la
// URL derivada de la misma base que usa el cliente HTTP.
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import PaginaContratos from '../presentation/features/contratos/PaginaContratos';

describe('PaginaContratos (RF-F09)', () => {
  it('embebe el Swagger sin salir de la consola', () => {
    render(
      <MemoryRouter>
        <PaginaContratos />
      </MemoryRouter>,
    );

    const marco = screen.getByTitle('Documentación Swagger de la API COELSA');
    expect(marco.tagName).toBe('IFRAME');
    // Sin VITE_API_URL (tests y dev con proxy) la URL es relativa a la API.
    expect(marco).toHaveAttribute('src', '/swagger/index.html');
  });
});
