import { defineComponent } from "vue";

export default defineComponent({
    template: `
        <nav class="navbar navbar-expand p-0">
            <div class="navbar-collapse">
                <ul class="navbar-nav h1">
                    <li>
                        <a class="dropdown-item" href="/">Homepage</a>
                        <a class="dropdown-item" href="/posts">Posts</a>
                        <a class="dropdown-item" href="/upload">Upload</a>
                        <a class="dropdown-item" href="/">Homepage</a>
                    </li>

                    <li class="nav-item">
                        <span class="mx-2">
                            |
                        </span>
                    <li>

                    <li class="nav-item">
                        <a class="nav-link h1 p-0" href="/upload">
                            Upload
                        </a>
                    </li>

                    <li class="nav-item mx-2">
                        &middot;
                    </li>

                    <li class="nav-item">
                        <a class="nav-link h1 p-0" href="/posts">
                            Posts
                        </a>
                    </li>
                </ul>
            </div>
        </nav>
    `,
});
