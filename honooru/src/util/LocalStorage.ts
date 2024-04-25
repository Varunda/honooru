import { Loadable, Loading } from "../Loading";

export class StorageEntry<T> {

    public constructor(data: T) {
        this.data = data;
    }

    public key: string = "";
    public timestamp: number = 0;
    public data: T
}

export default class LocalStorageUtil {

    private static storageAvailable: boolean | undefined = undefined;

    /**
     * check if localStorage is available 
     * 
     * @returns
     */
    public static available(): boolean {
        if (this.storageAvailable == undefined) {
            try {
                const storage = localStorage;
                const key: string = `$__storage_test__`;
                storage.setItem(key, key);
                storage.removeItem(key);
                this.storageAvailable = true;
            } catch (err) {
                this.storageAvailable = false;
                console.warn(`LocalStorage> localStorage is not available: ${err}`);
            }
        }

        return this.storageAvailable;
    }

    /**
     * try to get a value based on the key from localStorage, using a fallback if needed
     * 
     * @param key       key of the value to get from localStorage
     * @param fallback  fallback method called if the key is not in localStorage already
     * @param options   options when getting the value from localStorage, such as how old the value can be
     * @returns 
     */
    public static async tryGet<T>(key: string, fallback: () => Promise<Loading<T>>, options?: { maxAge?: number }): Promise<Loading<T>> {
        if (this.available() == false) {
            return fallback();
        }

        key = key.toLowerCase();

        let data: T | null = this.get(key, options);

        if (data == null) {
            console.log(`LocalStorage> data not found in storage, using fallback [key=${key}]`);
            const response: Loading<T> = await fallback();
            console.log(`LocalStorage> fallback done [key=${key}] [response.state=${response.state}]`);

            if (response.state == "loaded") {
                data = response.data;
                this.set(key, data);
            } else {
                return response;
            }
        }

        return Loadable.loaded(data);
    }

    /**
     * get a value from localStorage
     * 
     * @param key       key of the value to get from localStorage
     * @param options   optional options, such as specifying the maximum age of an item (in milliseconds)
     * @returns
     */
    public static get<T>(key: string, options?: {maxAge?: number } ): T | null {
        if (this.available() == false) {
            return null;
        }

        key = key.toLowerCase();
        const item: string | null = localStorage.getItem(key);
        if (item == null) {
            return null;
        }

        const data: StorageEntry<T> = JSON.parse(item) as StorageEntry<T>;
        if (data.key == undefined || data.timestamp == undefined) {
            console.log(`LocalStorage> found a non-StorageEntry<T> entry, removing [key=${key}]`);
            this.remove(key);
            return null;
        }

        //console.log(`LocalStorage> found key, checking age [key=${key}] [timestamp=${data.timestamp}] [age=${Date.now() - data.timestamp}] [maxAge = ${ options?.maxAge }]`);

        if (options?.maxAge) {
            if (Date.now() - data.timestamp > options.maxAge) {
                console.log(`LocalStorage> max age expired, removing item and returning null [key=${key}] [maxAge=${options.maxAge}]`);
                return null;
            }
        }

        return data.data;
    }

    /**
     * set a value in localStorage
     * 
     * @param key   key of the value to store into localStorage
     * @param data  data to be stored
     * @returns
     */
    public static set<T>(key: string, data: T): void {
        if (this.available() == false) {
            return;
        }

        key = key.toLowerCase();

        const entry: StorageEntry<T> = new StorageEntry(data);
        entry.key = key;
        entry.timestamp = Date.now();

        localStorage.setItem(key, JSON.stringify(entry));
    }

    /**
     * remove a stored key
     * 
     * @param key
     * @returns
     */
    public static remove(key: string): void {
        if (this.available() == false) {
            return;
        }

        console.log(`LocalStorage> explicitly removed '${key.toLowerCase()}' from localStorage`);

        localStorage.removeItem(key.toLowerCase());
    }



}