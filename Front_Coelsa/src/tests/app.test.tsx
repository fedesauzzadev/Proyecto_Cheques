import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import App from '../App';

describe('App', () => {
  it('muestra el título de la consola', () => {
    render(
      <MemoryRouter>
        <App />
      </MemoryRouter>,
    );
    expect(screen.getByRole('heading', { name: /consola de instrumentos/i })).toBeInTheDocument();
  });
});
