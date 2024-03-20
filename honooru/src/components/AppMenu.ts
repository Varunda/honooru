import Vue from "vue";

export const AppMenu = Vue.extend({
    template: `
        <nav class="navbar">
            <div class="navbar-brand">
                <a class="navbar-item" href="/">
                    <img src="/img/logo0.png" width="28" height="28">
                </a>
            </div>

            <div class="navbar-menu">
                <div class="navbar-start">
                    <a class="navbar-item" href="/">
                        PLAP
                    </a>

                    <a class="navbar-item" href="/posts">
                        posts
                    </a>

                    <a class="navbar-item" href="/upload">
                        upload
                    </a>
                </div>

                <div class="navbar-end">
                    <div class="navbar-item">
                        <a class="button is-primary">
                            signin
                        </a>
                    </div>
                </div>
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
                PLAP
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
            <a class="dropdown-item" href="/posts">Posts</a>
            <a class="dropdown-item" href="/upload">Upload</a>
            <a class="dropdown-item" href="/">Homepage</a>
        </li>
    `
});

export const MenuDropdown = Vue.extend({
    template: `
        <li class="nav-item dropdown">
            <menu-image></menu-image>
            <ul class="dropdown-menu mt-0">
                <slot>
                    <menu-homepage></menu-homepage>
                </slot>
            </ul>
        </li>
    `,

    components: {
        MenuImage, MenuHomepage
    }
});