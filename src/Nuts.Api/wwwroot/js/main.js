/* ============================================
   Ореховый Сад — Main JS
   Premium interactions & effects
   ============================================ */

document.addEventListener('DOMContentLoaded', () => {

  // =========================================
  // Floating Particles (Canvas)
  // =========================================
  const canvas = document.getElementById('particles');
  if (canvas) {
    const ctx = canvas.getContext('2d');
    let particles = [];
    let animId;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    class Particle {
      constructor() {
        this.reset();
      }
      reset() {
        this.x = Math.random() * canvas.width;
        this.y = Math.random() * canvas.height;
        this.size = Math.random() * 2 + 0.5;
        this.speedX = (Math.random() - 0.5) * 0.3;
        this.speedY = (Math.random() - 0.5) * 0.3;
        this.opacity = Math.random() * 0.15 + 0.05;
        // Golden tones
        const tone = Math.random();
        if (tone < 0.6) {
          this.color = `rgba(201, 168, 76, ${this.opacity})`;      // gold
        } else if (tone < 0.85) {
          this.color = `rgba(139, 105, 20, ${this.opacity})`;      // dark gold
        } else {
          this.color = `rgba(212, 184, 106, ${this.opacity * 0.7})`;// light gold
        }
      }
      update() {
        this.x += this.speedX;
        this.y += this.speedY;
        if (this.x < -10 || this.x > canvas.width + 10 ||
            this.y < -10 || this.y > canvas.height + 10) {
          this.reset();
        }
      }
      draw() {
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
        ctx.fillStyle = this.color;
        ctx.fill();
      }
    }

    const count = Math.min(80, Math.floor(window.innerWidth / 20));
    for (let i = 0; i < count; i++) {
      particles.push(new Particle());
    }

    const animate = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      particles.forEach(p => {
        p.update();
        p.draw();
      });
      animId = requestAnimationFrame(animate);
    };
    animate();

    // Pause particles when tab is hidden
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        cancelAnimationFrame(animId);
      } else {
        animate();
      }
    });
  }

  // =========================================
  // Header scroll effect
  // =========================================
  const header = document.getElementById('header');

  const onScroll = () => {
    header.classList.toggle('header--scrolled', window.scrollY > 60);
  };
  window.addEventListener('scroll', onScroll, { passive: true });

  // =========================================
  // Mobile menu
  // =========================================
  const burger = document.getElementById('burger');
  const mobileMenu = document.getElementById('mobileMenu');

  burger.addEventListener('click', () => {
    burger.classList.toggle('active');
    mobileMenu.classList.toggle('active');
    document.body.style.overflow = mobileMenu.classList.contains('active') ? 'hidden' : '';
  });

  mobileMenu.querySelectorAll('a').forEach(link => {
    link.addEventListener('click', () => {
      burger.classList.remove('active');
      mobileMenu.classList.remove('active');
      document.body.style.overflow = '';
    });
  });

  // =========================================
  // Reveal on scroll (IntersectionObserver)
  // =========================================
  const revealElements = document.querySelectorAll('.reveal');

  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const parent = entry.target.parentElement;
        const siblings = parent ? Array.from(parent.querySelectorAll(':scope > .reveal')) : [];
        const index = siblings.indexOf(entry.target);
        const delay = index >= 0 ? index * 120 : 0;

        setTimeout(() => {
          entry.target.classList.add('revealed');
        }, delay);

        revealObserver.unobserve(entry.target);
      }
    });
  }, {
    threshold: 0.12,
    rootMargin: '0px 0px -60px 0px'
  });

  revealElements.forEach(el => revealObserver.observe(el));

  // =========================================
  // Catalog category tabs
  // =========================================
  const tabs = document.querySelectorAll('.catalog__tab');
  const catalogItems = document.querySelectorAll('.catalog__item[data-category]');

  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      const cat = tab.dataset.category;

      catalogItems.forEach(item => {
        const cats = item.dataset.category.split(' ');
        if (cat === 'all' || cats.includes(cat)) {
          item.classList.remove('catalog__item--hidden');
        } else {
          item.classList.add('catalog__item--hidden');
        }
      });
    });
  });

  // =========================================
  // Counter animation (stats numbers)
  // =========================================
  const counters = document.querySelectorAll('.stats__number[data-target]');

  const counterObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const el = entry.target;
        const target = parseInt(el.dataset.target, 10);
        const suffix = el.dataset.suffix || '';
        const duration = 2000;
        const start = performance.now();

        const easeOutQuart = t => 1 - Math.pow(1 - t, 4);

        const step = (now) => {
          const elapsed = now - start;
          const progress = Math.min(elapsed / duration, 1);
          const value = Math.floor(easeOutQuart(progress) * target);
          el.textContent = value.toLocaleString('ru-RU') + suffix;
          if (progress < 1) {
            requestAnimationFrame(step);
          }
        };
        requestAnimationFrame(step);
        counterObserver.unobserve(el);
      }
    });
  }, { threshold: 0.5 });

  counters.forEach(el => counterObserver.observe(el));

  // =========================================
  // Smooth scroll for anchor links
  // =========================================
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', (e) => {
      const target = document.querySelector(anchor.getAttribute('href'));
      if (target) {
        e.preventDefault();
        const headerHeight = header.offsetHeight;
        const targetPosition = target.getBoundingClientRect().top + window.scrollY - headerHeight - 20;
        window.scrollTo({ top: targetPosition, behavior: 'smooth' });
      }
    });
  });

  // =========================================
  // Parallax on hero tree
  // =========================================
  const treeWrapper = document.querySelector('.hero__tree-wrapper');
  if (treeWrapper) {
    window.addEventListener('scroll', () => {
      const scrollY = window.scrollY;
      if (scrollY < window.innerHeight) {
        treeWrapper.style.transform = `translateY(${scrollY * 0.15}px)`;
      }
    }, { passive: true });
  }

  // =========================================
  // Mouse glow on cards
  // =========================================
  const cards = document.querySelectorAll('.philosophy__card, .catalog__item, .media__card');
  cards.forEach(card => {
    card.addEventListener('mousemove', (e) => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      card.style.background = `radial-gradient(400px circle at ${x}px ${y}px, rgba(139,105,20,0.07), var(--black-card))`;
    });
    card.addEventListener('mouseleave', () => {
      card.style.background = '';
    });
  });

  // =========================================
  // Magnetic effect on CTA buttons
  // =========================================
  const magneticButtons = document.querySelectorAll('.hero__cta, .catalog__button, .contact__submit');
  magneticButtons.forEach(btn => {
    btn.addEventListener('mousemove', (e) => {
      const rect = btn.getBoundingClientRect();
      const x = e.clientX - rect.left - rect.width / 2;
      const y = e.clientY - rect.top - rect.height / 2;
      btn.style.transform = `translate(${x * 0.15}px, ${y * 0.15}px)`;
    });
    btn.addEventListener('mouseleave', () => {
      btn.style.transform = '';
      btn.style.transition = 'transform 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
      setTimeout(() => { btn.style.transition = ''; }, 400);
    });
  });

  // =========================================
  // Custom cursor dot (desktop only)
  // =========================================
  if (window.matchMedia('(pointer: fine)').matches) {
    const cursor = document.createElement('div');
    cursor.className = 'custom-cursor';
    document.body.appendChild(cursor);

    let cursorX = 0, cursorY = 0;
    let dotX = 0, dotY = 0;

    document.addEventListener('mousemove', (e) => {
      cursorX = e.clientX;
      cursorY = e.clientY;
    });

    const moveCursor = () => {
      dotX += (cursorX - dotX) * 0.15;
      dotY += (cursorY - dotY) * 0.15;
      cursor.style.transform = `translate(${dotX - 6}px, ${dotY - 6}px)`;
      requestAnimationFrame(moveCursor);
    };
    moveCursor();

    // Grow on interactive elements
    const interactives = document.querySelectorAll('a, button, .catalog__item, .philosophy__card, .media__card');
    interactives.forEach(el => {
      el.addEventListener('mouseenter', () => cursor.classList.add('custom-cursor--active'));
      el.addEventListener('mouseleave', () => cursor.classList.remove('custom-cursor--active'));
    });
  }

  // =========================================
  // Contact form
  // =========================================
  const form = document.getElementById('contactForm');
  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      const btn = form.querySelector('.contact__submit span');
      const originalText = btn.textContent;
      btn.textContent = 'Отправка...';

      try {
        const body = {
          name: form.querySelector('[name="name"]').value,
          phone: form.querySelector('[name="phone"]').value,
          email: form.querySelector('[name="email"]').value || null
        };
        const res = await fetch('/api/contacts', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body)
        });
        btn.textContent = res.ok ? 'Отправлено!' : 'Ошибка';
        if (res.ok) form.reset();
      } catch {
        btn.textContent = 'Ошибка сети';
      }
      setTimeout(() => { btn.textContent = originalText; }, 2500);
    });
  }

});
