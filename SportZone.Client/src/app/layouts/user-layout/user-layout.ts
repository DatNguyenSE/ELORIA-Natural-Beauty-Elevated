import { Component } from '@angular/core';
import { RouterOutlet } from "@angular/router";
import { Footer } from "../footer/footer";
import { Nav } from "../nav/nav";

@Component({
  selector: 'app-user-layout',
  imports: [RouterOutlet, Footer, Nav],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.css',
})
export class UserLayout {

}
