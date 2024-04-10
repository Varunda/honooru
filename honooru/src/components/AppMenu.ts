import Vue from "vue";

import AccountUtil from "util/AccountUtil";

export const AppMenu = Vue.extend({
    template: `
        <nav class="navbar navbar-expand bg-body-tertiary pt-0 mb-2 container-fluid-negative px-3">
            <div class="navbar-collapse">
                <a class="navbar-brand py-0" href="#">
                    <img src="/img/logo0.png" style="height: 100%; width: 48px;" title="homepage" />
                    PLAP
                </a>
                <ul class="navbar-nav">
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                            pages
                        </a>
                        <ul class="dropdown-menu">
                            <li><a class="dropdown-item" href="/">Homepage</a></li>
                            <li><a class="dropdown-item" href="/posts">Posts</a></li>
                            <li><a class="dropdown-item" href="/upload">Upload</a></li>
                            <li><a class="dropdown-item" href="/">Homepage</a></li>
                        </ul>
                    </li>

                    <li class="nav-item">
                        <a class="nav-link" href="/posts">
                            posts
                        </a>
                    </li>

                    <li class="nav-item">
                        <a class="nav-link" href="/upload">
                            upload
                        </a>
                    </li>

                </ul>

                <ul class="navbar-nav ms-auto">
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                            {{accountName}}
                        </a>
                        <ul class="dropdown-menu dropdown-menu-right">
                            <li><a class="dropdown-item" href="/settings">settings</a></li>
                            <li><a class="dropdown-item" href="/posts">logout</a></li>
                        </ul>
                    </li>
                </ul>

            </div>
        </nav>
    `,

    computed: {
        accountName: function(): string {
            return AccountUtil.get().name;
        }
    }
});
