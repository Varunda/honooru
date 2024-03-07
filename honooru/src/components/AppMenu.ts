import Vue from "vue";

export const AppMenu = Vue.extend({
    template: `
        <nav class="navbar navbar-expand p-0">
            <div class="navbar-collapse">
                <ul class="navbar-nav h1">
                    <slot></slot>
                </ul>
            </div>
        </nav>
    `,
});

export const MenuSep = Vue.extend({
    template: `
        <li class="nav-item h1 p-0 mx-2">/</li>
    `
});

export const MenuImage = Vue.extend({
    template: `
        <li class="nav-item">
            <a class="nav-link dropdown-toggle h1 p-0" href="/" data-toggle="dropdown">
                <img :src="'/img/logo' + this.ID + '.png'" style="height: 100%; width: 48px;" title="homepage" />
                App
            </a>
        </li>
    `,

    data: function() {
        return {
            ID: 0 as number
        }
    },
});

export const MenuHomepage = Vue.extend({
    template: `
        <li>
            <a class="dropdown-item" href="/">Homepage</a>
        </li>
    `
});

export const MenuOutfits = Vue.extend({
    template: `
        <li class="dropdown-submenu">
            <span class="dropdown-item dropdown-toggle">Outfits</span>
            <ul class="dropdown-menu">
                <li><a class="dropdown-item" href="/outfitfinder">Search</a></li>
                <li><a class="dropdown-item" href="/report">Report</a></li>
                <li><a class="dropdown-item" href="/outfitpop">Population</a></li>
            </ul>
        </li>
    `,
});

export const MenuDropdown = Vue.extend({
    template: `
        <li class="nav-item dropdown">
            <menu-image></menu-image>
            <ul class="dropdown-menu mt-0">
                <slot>
                    <menu-homepage></menu-homepage>
                    <menu-realtime></menu-realtime>
                </slot>
            </ul>
        </li>
    `,

    components: {
        MenuImage, MenuHomepage
    }
});