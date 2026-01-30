import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
    selector: 'app-navbar',
    standalone: true,
    imports: [RouterLink, RouterLinkActive],
    templateUrl: './navbar.component.html',
    styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
    navItems = [
        { path: '/', label: 'Home', icon: 'home' },
        { path: '/schedule', label: 'Schedule', icon: 'calendar' },
        { path: '/results', label: 'Results', icon: 'trophy' },
        { path: '/predictions', label: 'Predictions', icon: 'target' },
        { path: '/more', label: 'More', icon: 'menu' }
    ];
}
