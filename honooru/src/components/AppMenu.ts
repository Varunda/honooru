﻿import Vue from "vue";

export const AppMenu = Vue.extend({
    template: `
        <nav class="navbar navbar-expand">
            <div class="navbar-collapse">
                <ul class="navbar-nav">
                    <li class="nav-item">
                        <a class="nav-link">
                            <img src="/img/logo0.png" style="height: 100%; width: 48px;" title="homepage" />
                            PLAP
                        </a>
                    </li>

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
            </div>
        </nav>
    `,
});
