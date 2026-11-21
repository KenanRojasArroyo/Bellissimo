// Dinamismo de Bellissimo: sin dependencias, respeta prefers-reduced-motion.
(() => {
    const root = document.documentElement;
    const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Activamos la clase 'js' para que CSS sepa que puede ocultar los elementos antes de animarlos
    root.classList.add('js');

    // Barra de navegación: se vuelve cristal al hacer scroll
    const header = document.querySelector('header');
    const onScroll = () => header && header.classList.toggle('is-scrolled', window.scrollY > 8);
    addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    // Tarjetas: la luz sigue al cursor (¡Corregido el error de sintaxis aquí!)
    document.addEventListener('pointermove', e => {
        const card = e.target.closest && e.target.closest('.card');
        if (!card) return;
        const r = card.getBoundingClientRect();
        card.style.setProperty('--mx', (e.clientX - r.left) + 'px');
        card.style.setProperty('--my', (e.clientY - r.top) + 'px');
    }, { passive: true });

    // Entrada escalonada de bloques al aparecer en pantalla
    if (reduce || !('IntersectionObserver' in window)) { 
        root.classList.remove('js'); 
        return; 
    }

    // Selecciona los elementos a animar (h1, tarjetas, y los campos del formulario)
    const targets = document.querySelectorAll(
        'main h1, main .hero > *, main .card, main .alert, main .table-responsive, main form > [class*="col-"], main dl.row'
    );

    const io = new IntersectionObserver(entries => {
        entries.forEach(en => {
            if (!en.isIntersecting) return;
            en.target.classList.add('is-in');
            io.unobserve(en.target);
            // Se retira la clase para que el hover de la tarjeta no herede el retraso
            setTimeout(() => en.target.classList.remove('reveal', 'is-in'), 1500);
        });
    }, { threshold: 0.12 });

    targets.forEach((el, i) => {
        el.style.setProperty('--d', Math.min(i, 8) * 70 + 'ms');
        el.classList.add('reveal');
        io.observe(el);
    });
})();